using Google.OrTools.ConstraintSolver;
using Helpers;
using Newtonsoft.Json;

namespace ORToolsSolver.VRP;

public class VRPSolver
{
    //insert params of service
    private readonly string _json;

    public VRPSolver(string json)
    {
        _json = json;
    }

    //private class DataModel
    //{
    //    public int VehicleNumber = 20;
    //    public int Depot = 0;
    //}

    private static long[,] ConvertListToMultidimensionalArray(List<long[]> listOfArrays)
    {
        int rows = listOfArrays.Count;
        int cols = listOfArrays[0].Length; // Assuming all arrays in the list have the same length
        long[,] multidimensionalArray = new long[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                multidimensionalArray[i, j] = listOfArrays[i][j];
            }
        }

        return multidimensionalArray;
    }

    private static long PrintSolution(RoutingModel routing, RoutingIndexManager manager, Assignment solution)
    {
        //Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

        // Inspect solution.
        long maxRouteDistance = 0;
        for (int i = 0; i < manager.GetNumberOfVehicles(); i++)
        {
            //Console.WriteLine("Route for Vehicle {0}:", i);
            long routeDistance = 0;
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                //Console.Write("{0} -> ", manager.IndexToNode((int)index));
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            //Console.WriteLine("{0}", manager.IndexToNode((int)index));
            //Console.WriteLine("Distance of the route: {0}m", routeDistance);
            maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
        }
        //Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
        return maxRouteDistance;
    }


    public ResultObject SolveVehicleRoutingProblem()
    {
        Root deserializedData = JsonConvert.DeserializeObject<Root>(_json);
        string errorMessage = string.Empty;
        if (deserializedData is null)
            Console.WriteLine("No data where sent through the API. Please check again!");
        if (deserializedData.JobData.Locations.Count == 0)
            errorMessage += "No locations found on the JSON. Please check again!\n";
        if (deserializedData.JobData.VehicleNumber <= 0)
            errorMessage += "Vehicle number must be a positive integer. Please check again!\n";
        if (deserializedData.JobData.MaxDistance < 0)
            errorMessage += "Vehicle maximum distance must be a non-negative decimal. Please check again!\n";

        ResultObject resultObject;
        DateTime? initialTime = null;
        if (deserializedData.Metadata is not null)
            initialTime = deserializedData.Metadata.Timestamp;

        if (string.IsNullOrEmpty(errorMessage))
        {
            //DataModel data = new DataModel();
            long maxDistance = deserializedData.JobData.MaxDistance;
            int vehicleNumber = deserializedData.JobData.VehicleNumber;
            int numThreads = Math.Min(vehicleNumber, 4);
            var locations = deserializedData.JobData.Locations;
            var numLocations = locations.Count;
            var orderedLocations = locations.AsEnumerable().OrderBy(r => r.Latitude).ThenBy(r => r.Longitude).ToArray();
            Location[][] locationsPerThread = new Location[numThreads][];
            int sizePerThread = numLocations / numThreads;
            int carsPerThread = vehicleNumber / numThreads;
            long result = 0;
            Parallel.For(0, numThreads, i =>
            {
                long res = result;
                //Console.WriteLine($"current thread {Environment.CurrentManagedThreadId}");
                int size = sizePerThread;
                int cars = carsPerThread;
                int upSize = size;
                int normalizedCars = cars;
                if (i == numThreads - 1)
                {
                    upSize = numLocations - i * sizePerThread;
                    normalizedCars = vehicleNumber - i * carsPerThread;
                }

                locationsPerThread[i] = new Location[upSize];
                var maxSize = Math.Min((i + 1) * size, numLocations);

                for (int j = i * size; j < i * size + upSize; j++)
                    locationsPerThread[i][j - i * size] = orderedLocations[j];

                Location[] currentLocations = locationsPerThread[i];
                List<long[]> tempDistanceMatrix = new List<long[]>();
                foreach (Location locOuter in currentLocations)
                {
                    List<long> distanceList = new List<long>();
                    foreach (Location locInner in currentLocations)
                    {
                        long distance = (long)Math.Round(HaversineCalculator.CalculateHaversineDistance(locOuter, locInner) * 1000.0, 3);
                        distanceList.Add(distance);
                    }
                    tempDistanceMatrix.Add(distanceList.ToArray());
                }

                var distanceMatrix = ConvertListToMultidimensionalArray(tempDistanceMatrix);
                // Create Routing Index Manager
                RoutingIndexManager manager =
                    new RoutingIndexManager(distanceMatrix.GetLength(0), normalizedCars, 0);

                // Create Routing Model.
                RoutingModel routing = new RoutingModel(manager);

                // Create and register a transit callback.
                int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to
                    // distance matrix NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return distanceMatrix[fromNode, toNode];
                });

                // Define cost of each arc.
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // Add Distance constraint.
                routing.AddDimension(transitCallbackIndex, 0, maxDistance,
                                     true, // start cumul to zero
                                     "Distance");
                RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
                distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Setting first solution heuristic.
                RoutingSearchParameters searchParameters =
                    operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                var solution = routing.SolveWithParameters(searchParameters);

                if (!Equals(solution, null))
                {
                    res = PrintSolution(routing, manager, solution);
                    result += res;
                }
                else
                    Console.WriteLine("No solution found!");
            });

            TimeSpan solutionTime = new TimeSpan();
            if (initialTime.HasValue)
            {
                solutionTime = DateTime.Now - initialTime.Value;
            }
            resultObject = new ResultObject
            {
                Success = true,
                Result = result,
                Duration = initialTime.HasValue ? solutionTime.ToString() : string.Empty,
            };
        }
        else
        {
            Console.WriteLine(errorMessage);
            resultObject = new ResultObject
            {
                Success = false,
                Info = errorMessage
            };

        }
        return resultObject;
    }

    #region Helpers
    private static class HaversineCalculator
    {
        public static double CalculateHaversineDistance(
          Location coordinate1,
          Location coordinate2)
        {
            double radians1 = DegreesToRadians(coordinate1.Latitude);
            double radians2 = DegreesToRadians(coordinate1.Longitude);
            double radians3 = DegreesToRadians(coordinate2.Latitude);
            double radians4 = DegreesToRadians(coordinate2.Longitude);
            double num1 = radians3 - radians1;
            double num2 = radians4 - radians2;
            double d = Math.Pow(Math.Sin(num1 / 2.0), 2.0) + Math.Cos(radians1) * Math.Cos(radians3) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6371.0 * (2.0 * Math.Atan2(Math.Sqrt(d), Math.Sqrt(1.0 - d)));
        }

        public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
    #endregion

    #region Deserialization Classes
    private class JobData
    {
        public List<Location> Locations { get; set; }
        public int VehicleNumber { get; set; }
        public long MaxDistance { get; set; }
    }

    private class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
    private class Metadata
    {
        public DateTime Timestamp { get; set; }
        public int Id { get; set; }
    }

    private class Root
    {
        public required JobData JobData { get; set; }
        public Metadata? Metadata { get; set; }
        public required Enums.ProblemType ProblemType { get; set; }

    }
    #endregion

    #region Serialization Classes
    public class ResultObject
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public long Result { get; set; } = 0;

        [JsonProperty("duration")]
        public string Duration { get; set; } = string.Empty;

        [JsonProperty("info")]
        public string Info { get; set; } = string.Empty;
    }
    #endregion
}
