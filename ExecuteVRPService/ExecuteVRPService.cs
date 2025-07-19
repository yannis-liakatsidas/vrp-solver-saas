// SendData Service
using System;
using System.Text;
using Helpers;
using Newtonsoft.Json;

namespace ExecuteVRPService;

public class ExecuteVRPService
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Please provide the path to the JSON file as a command-line argument.");
            return;
        }

        if (args.Length > 3)
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

            var jsonToSend = ModifyJson(jsonObject, int.Parse(args[1]), decimal.Parse(args[2]));
            json = JsonConvert.SerializeObject(jsonToSend, Formatting.Indented);

            Console.WriteLine($"Sending request for solving...");

            var solver = new ORToolsSolver.VRP.VRPSolver(json);

            var startTime = DateTime.Now;
            var result = solver.SolveVehicleRoutingProblem();
            var distance = result.Result;
            if (distance > 0)
                Console.WriteLine($"Request solved and result received. Maximum covered distance equals to {distance} m.");
            else
                Console.WriteLine("No feasible solution found!");

            var timespan = DateTime.Now - startTime;
            Console.WriteLine($"Total time elapsed: {timespan.Minutes:00}:{timespan.Seconds:00}:{timespan.Milliseconds:000} mins.");
        };
    }

    static ProblemData ModifyJson(Dictionary<string, object> initialJson, int vehicleNumber, decimal maxDistance)
    {
        var finalJson = new ProblemData
        {
            JobData = new Dictionary<string, object>()
            {
                {initialJson.Keys.First(), initialJson.Values.First()},
                { "VehicleNumber", vehicleNumber },
                { "MaxDistance",  maxDistance }
            },
            Metadata = new Metadata
            {
                Id = new Random().Next(),
                Timestamp = DateTime.Now,
            },
            ProblemType = Enums.ProblemType.VRP
        };
        return finalJson;
    }
}