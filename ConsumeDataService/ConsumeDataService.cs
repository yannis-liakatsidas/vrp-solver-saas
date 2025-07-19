using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using ORToolsSolver.VRP;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace ConsumeDataService;

public class ConsumeDataService()
{

    private const string serviceName = "Solver Dispatcher";
    private const string firstActionPerformed = "Sending to solver for execution";
    private const string lastActionPerformed = "Added to results queue";
    private static readonly Dictionary<Enums.ProblemType, string> queueMapping = new Dictionary<Enums.ProblemType, string>
        {
            { Enums.ProblemType.VRP, "VRP_queue" },
            { Enums.ProblemType.Knapsack, "Knapsack_queue" },
            { Enums.ProblemType.CpSat, "CpSat_queue" }
        };

    private static readonly List<int> jobsQueue = [];
    private static readonly List<int> resultsQueue = [];
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    static async Task Main(string[] args)
    {
        var factory = new ConnectionFactory
        {
            UserName = "test1",
            Password = "test1",
            HostName = "10.3.2.76"
        };

        //var factory = new ConnectionFactory
        //{
        //    HostName = "localhost"
        //};
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var consumer = new EventingBasicConsumer(channel);

        foreach (var queueInfo in queueMapping)
        {
            channel.QueueDeclareNoWait(queue: queueInfo.Value,
                 durable: false,
                 exclusive: false,
                 autoDelete: false,
                 arguments: null);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            while (true)
            {
                var result = channel.BasicGet(queueInfo.Value, autoAck: false);
                if (result is null)
                    break; // No more messages on the queue

                await ProcessMessageAsync(channel, result.Body.ToArray(), result.BasicProperties, result.DeliveryTag).ConfigureAwait(false);
            }

            channel.BasicConsume(queue: queueInfo.Value,
                                 autoAck: false,
                                 consumer: consumer);
        }

        consumer.Received += async (model, ea) =>
        {
            await ProcessMessageAsync(channel, ea.Body.ToArray(), ea.BasicProperties, ea.DeliveryTag).ConfigureAwait(false);
        };

        Console.ReadLine();
    }

    private static async Task ProcessMessageAsync(IModel channel, byte[] body, IBasicProperties props, ulong deliveryTag)
    {
        string response = string.Empty;
        var replyProps = channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;
        replyProps.Headers = new Dictionary<string, object>
            {
                { "IdNumber", props.Headers["IdNumber"] }
            };
        var message = Encoding.UTF8.GetString(body);
        var originalData = JsonConvert.DeserializeObject<ProblemData>(message);

        string queueName = string.Empty;
        string exchangeName = string.Empty;
        byte[]? exchangeNameBytes = props.Headers["ExchangeName"] as byte[];
        if (exchangeNameBytes is not null)
            exchangeName = Encoding.UTF8.GetString(exchangeNameBytes);
        int id = (int)props.Headers["IdNumber"];
        jobsQueue.Add(id);

        string usedQueue = string.Empty;

        switch (originalData.ProblemType)
        {
            case Enums.ProblemType.VRP:
                usedQueue = queueMapping[Enums.ProblemType.VRP];
                break;

            case Enums.ProblemType.Knapsack:
                usedQueue = queueMapping[Enums.ProblemType.Knapsack];
                break;

            case Enums.ProblemType.CpSat:
                usedQueue = queueMapping[Enums.ProblemType.CpSat];
                break;

            default:
                break;
        }

        var solver = new VRPSolver(message);
        string apiUrl = "http://10.3.2.75:80/api/main/runjob";
        //string apiUrl = "http://localhost:50000/api/main/runjob";

        VRPSolver.ResultObject? errorObject = null;

        if (await semaphore.WaitAsync(-1).ConfigureAwait(false))
        {
            Console.WriteLine($"Jobs Queue: {string.Join(", ", jobsQueue.Select(r => r))}");
            Console.WriteLine($"Results Queue: {string.Join(", ", resultsQueue.Select(r => r))}");
            int index = id;
            try
            {
                Console.WriteLine($"Sending job {id} for execution...");

                var currentTime = DateTime.Now;

                // Get the time zone of Greece (GMT+2)
                TimeZoneInfo greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

                // Convert the server timestamp to Greece local time
                DateTime greeceLocalTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, greeceTimeZone);

                var timestamp = greeceLocalTime.ToString("dd/MM/yyyy, HH:mm:ss");

                var newRecord = new CsvData()
                {
                    Timestamp = timestamp,
                    ServiceName = serviceName,
                    ActionPerformed = firstActionPerformed,
                    JobId = id
                };
                var loggingData = JsonConvert.SerializeObject(newRecord);
                await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
                //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL
                HttpResponseMessage apiResponse;
                apiResponse = await ApiFunctions.SendDataToAPIAsync(message, apiUrl).ConfigureAwait(false);
                if (apiResponse.IsSuccessStatusCode)
                {
                    response = apiResponse.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"Job {index} finished successfully.");
                }
                else
                {
                    Console.WriteLine($"Job {index} finished with errors.");
                }

                var responseBytes = Encoding.UTF8.GetBytes(response.ToCharArray());

                channel.BasicPublish(exchange: exchangeName,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                body: responseBytes);
                channel.BasicAck(deliveryTag: deliveryTag, multiple: false);

            }
            catch (Exception e)
            {
                Console.WriteLine($" Exception occurred during job {index}: {e.Message}");
                errorObject = new VRPSolver.ResultObject { Success = false, Info = e.Message };

                if (string.IsNullOrEmpty(response))
                    response = JsonConvert.SerializeObject(errorObject);
               
                channel.BasicReject(deliveryTag: deliveryTag, requeue: false);
            }
            finally
            {
                var currentTime = DateTime.Now;

                // Get the time zone of Greece (GMT+2)
                TimeZoneInfo greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

                // Convert the server timestamp to Greece local time
                DateTime greeceLocalTime = TimeZoneInfo.ConvertTime(currentTime, TimeZoneInfo.Local, greeceTimeZone);

                var timestamp = greeceLocalTime.ToString("dd/MM/yyyy, HH:mm:ss");

                var newRecord = new CsvData()
                {
                    Timestamp = timestamp,
                    ServiceName = serviceName,
                    ActionPerformed = lastActionPerformed,
                    JobId = index
                };

                var loggingData = JsonConvert.SerializeObject(newRecord);
                await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
                //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

                jobsQueue.Remove(index);
                resultsQueue.Add(index);

                semaphore.Release();
            }
        }
    }
}


