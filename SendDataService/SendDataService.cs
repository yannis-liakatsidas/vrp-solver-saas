// SendData Service
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Helpers;
using Newtonsoft.Json;
using static Helpers.Enums;
using static ORToolsSolver.VRP.VRPSolver;

namespace SendDataService
{

    public class SendDataService
    {
        private static readonly string filePath = "executionNumber.txt";
        private static readonly string resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "results.csv");
        private static readonly object lockObject = new object();
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private const string serviceName = "Client Side";
        private const string firstActionPerformed = "Sending request to message broker";
        private const string lastActionPerformed = "Result received";

        private class ResultsCsvData
        {
            public int JobId { get; set; }
            public bool Success { get; set; }
        }
        public static int GetNextNumber()
        {
            lock (lockObject)
            {
                int currentNumber = GetCurrentNumber();
                int nextNumber = currentNumber + 1;
                UpdateExecutionNumber(nextNumber);
                return nextNumber;
            }
        }

        private static int GetCurrentNumber()
        {
            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                if (int.TryParse(content, out int currentNumber))
                {
                    return currentNumber;
                }
            }
            return 0;
        }

        private static void UpdateExecutionNumber(int newNumber)
        {
            File.WriteAllText(filePath, newNumber.ToString());
        }
        static async Task Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Please provide the path to the JSON file as a command-line argument.");
                return;
            }

            if (args.Length > 4)
            {
                Console.WriteLine("Too many arguments were provided. Please provide the path to the JSON file only.");
                return;
            }

            string jsonFilePath = args[0];

            // Load JSON from file
            string json = File.ReadAllText(jsonFilePath);
            // Convert JSON to Dictionary<string, object>
            if (json is not null && json.Length > 0)
            {
                Dictionary<string, object> jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (Enum.TryParse(typeof(ProblemType), args[1], out object? type))
                {
                    int idNumber = GetNextNumber();

                    var jsonToSend = ModifyJson(jsonObject, (ProblemType)type, idNumber, int.Parse(args[2]), long.Parse(args[3]));
                    json = JsonConvert.SerializeObject(jsonToSend, Formatting.Indented);

                    //for Architecture 3 (full)
                    string apiUrl = "http://10.3.2.76:80/api/main/runjob";
                    //string apiUrl = "http://localhost:60000/api/main/runjob";

                    //for Architecture 2
                    //string apiUrl = "http://10.3.2.75:80/api/main/runjob";
                    //string apiUrl = "http://localhost:50000/api/main/runjob";

                    var timestamp = DateTime.Now.ToString("dd/MM/yyyy, HH:mm:ss");
                    var newRecord = new CsvData()
                    {
                        Timestamp = timestamp,
                        ServiceName = serviceName,
                        ActionPerformed = firstActionPerformed,
                        JobId = idNumber
                    };

                    var loggingData = JsonConvert.SerializeObject(newRecord);
                    await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
                    //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

                    Console.WriteLine($"Sending request {idNumber}...");

                    var responseMessage = await ApiFunctions.SendDataToAPIAsync(json, apiUrl).ConfigureAwait(false);
                    var response = await responseMessage.Content.ReadAsStringAsync();

                    timestamp = DateTime.Now.ToString("dd/MM/yyyy, HH:mm:ss");
                    newRecord = new CsvData()
                    {
                        Timestamp = timestamp,
                        ServiceName = serviceName,
                        ActionPerformed = lastActionPerformed,
                        JobId = idNumber
                    };

                    loggingData = JsonConvert.SerializeObject(newRecord);
                    await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
                    //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

                    Console.WriteLine($"Request {idNumber}: result received.\n{response}");
                }
            }
        }

        #region Helpers
        private static ProblemData ModifyJson(Dictionary<string, object> initialJson, ProblemType problemType, int idNumber, int numVehicles, long maxDistance)
        {
            var finalJson = new ProblemData
            {
                JobData = new Dictionary<string, object>()
                {
                    {initialJson.Keys.First(), initialJson.Values.First()},
                    {"VehicleNumber", numVehicles },
                    {"MaxDistance", maxDistance }
                },
                Metadata = new Metadata
                {
                    Id = idNumber,
                    Timestamp = DateTime.Now,
                },
                ProblemType = problemType
            };
            return finalJson;
        }
        #endregion
    }
}