using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Helpers;
using Newtonsoft.Json;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Reflection;

namespace RabbitMQns.Packages;

public sealed class RpcClient : IDisposable
{
    //private readonly Dictionary<Guid, string> clientQueues = new Dictionary<Guid, string>();
    private readonly IConnection connection;
    private readonly IModel channel;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();
    private static readonly List<int> jobsQueue = [];
    private static readonly List<int> resultsQueue = [];

    //change into SINGLETON DESIGN PATTERN & test
    private static readonly Lazy<RpcClient> lazyRpcClientInstance = new Lazy<RpcClient>(() => new RpcClient());

    private const string serviceName = "Solver Controller";
    private const string firstActionPerformed = "Request received & added to queue";
    private const string lastActionPerformed = "Sending to requester";

    // Private constructor to prevent instantiation
    private RpcClient()
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
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
    }

    public static RpcClient GetOrCreateRpcClient => lazyRpcClientInstance.Value;

    public async Task<Task<string>> CallAsync(ProblemData problemData, CancellationToken cancellationToken = default)
    {
        //ProblemData problemData = JsonConvert.DeserializeObject<ProblemData>(message);

        string exchangeName = "Demo_Exchange";
        string replyQueueName = string.Empty;
        string queueName = string.Empty;
        string key = string.Empty;
        string replyKey = string.Empty;

        switch (problemData.ProblemType)
        {
            case Enums.ProblemType.VRP:
                replyQueueName = "VRP_client_queue";
                queueName = "VRP_queue";
                key = "vrp-key";
                replyKey = "vrp-key-reply";
                break;

            case Enums.ProblemType.Knapsack:
                replyQueueName = "Knapsack_client_queue";
                queueName = "Knapsack_queue";
                key = "knapsack-key";
                replyKey = "knapsack-key-reply";
                break;

            case Enums.ProblemType.CpSat:
                replyQueueName = "CpSat_client_queue";
                queueName = "CpSat_queue";
                key = "cpsat-key";
                replyKey = "cpsat-key-reply";
                break;

            default:
                break;
        }

        channel.ExchangeDeclareNoWait(exchangeName, ExchangeType.Direct, false, true, null);
        channel.QueueDeclare(replyQueueName, exclusive: false);
        channel.QueueBindNoWait(queueName, exchangeName, key, null);
        channel.QueueBindNoWait(replyQueueName, exchangeName, replyKey, null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                return;
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);
            var properties = ea.BasicProperties;
            int id = (int)properties.Headers["IdNumber"];
            //jobsQueue = ((List<object>)properties.Headers["JobsQueue"]).Cast<int>().ToList();
            //resultsQueue = ((List<object>)properties.Headers["ResultsQueue"]).Cast<int>().ToList();
            jobsQueue.Remove(id);
            resultsQueue.Add(id);
            Console.WriteLine($"Jobs Queue: {string.Join(", ", jobsQueue.Select(r => r))}");
            Console.WriteLine($"Results Queue: {string.Join(", ", resultsQueue.Select(r => r))}");

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
                JobId = id
            };

            var loggingData = JsonConvert.SerializeObject(newRecord);
            await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
            //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

            Console.WriteLine($"Sending result {id} to requester...");
        };

        channel.BasicConsume(consumer: consumer,
                             queue: replyQueueName,
                             autoAck: true);

        IBasicProperties props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyKey; //routing key
        int currentId = problemData.Metadata.Id;
        jobsQueue.Add(currentId);

        props.Headers = new Dictionary<string, object>()
        {
            { "ExchangeName", exchangeName },
            { "IdNumber", currentId }
            //{ "JobsQueue", jobsQueue },
            //{"ResultsQueue", resultsQueue }
        };

        var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(problemData));
        var taskCompletionSource = new TaskCompletionSource<string>();

        channel.BasicPublish(exchange: exchangeName,
                             routingKey: key,
                             basicProperties: props,
                             body: messageBytes);

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
            JobId = currentId
        };

        var loggingData = JsonConvert.SerializeObject(newRecord);
        await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
        //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

        Console.WriteLine($"Jobs Queue: {string.Join(", ", jobsQueue.Select(r => r))}");
        Console.WriteLine($"Results Queue: {string.Join(", ", resultsQueue.Select(r => r))}");

        callbackMapper.TryAdd(correlationId, taskCompletionSource);
        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
        return taskCompletionSource.Task;
    }

    public void Dispose()
    {
        channel.Close();
        connection.Close();
    }
}