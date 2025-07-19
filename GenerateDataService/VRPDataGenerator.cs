using System.Runtime.CompilerServices;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataGenerator.VRP;

public class VRPDataGenerator
{
    private int _numLocations;
    private int? _numVehicles;
    private bool _hasTimeWindows;
    private bool _hasCapacities;
    private bool _hasDemands;

    public VRPDataGenerator(
      int numLocations,
      int? numVehicles = null,
      bool hasDemands = false,
      bool hasCapacities = false,
      bool hasTimeWindows = false)
    {
        _numLocations = numLocations;
        _numVehicles = numVehicles;
        _hasDemands = hasDemands;
        _hasCapacities = hasCapacities;
        _hasTimeWindows = hasTimeWindows;
    }

    public class TupleConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var tuple = (ITuple)value;
            var jsonObject = new JObject();

            for (int i = 0; i < tuple.Length; i++)
            {
                if (tuple[i] is not null)
                {
                    string propertyName = GetCustomPropertyName(i); // Replace with your logic to get custom names
                    jsonObject.Add(propertyName, JToken.FromObject(tuple[i], serializer));
                }
            }

            jsonObject.WriteTo(writer);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        private string GetCustomPropertyName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Latitude";
                case 1:
                    return "Longitude";
                // Add more cases for other indices
                default:
                    return $"Item{index + 1}";
            }
        }
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(ITuple).IsAssignableFrom(objectType);
        }
    }

    public Dictionary<string, object> GenerateData()
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        // Generate random coordinates within a city boundary
        List<(double Latitude, double Longitude)> locations = new List<(double Latitude, double Longitude)>();
        double athensLatMin = 37.92; // Latitude range for Athens Metropolitan Area
        double athensLatMax = 38.02;
        double athensLonMin = 23.70; // Longitude range for Athens Metropolitan Area
        double athensLonMax = 23.80;

        Random random = new Random();
        for (int i = 0; i < _numLocations; i++)
        {
            double lat = random.NextDouble() * (athensLatMax - athensLatMin) + athensLatMin;
            double lon = random.NextDouble() * (athensLonMax - athensLonMin) + athensLonMin;
            locations.Add((Latitude: lat, Longitude: lon));
        }
        data["Locations"] = locations.ToArray();

        if (_hasDemands)
        {
            List<int> demands = new List<int>();
            for (int i = 0; i < _numLocations; i++)
                demands.Add(random.Next(1, 51));
            data["Demands"] = demands.ToArray();
        }

        // Generate random vehicle capacities
        if (_hasCapacities && _numVehicles is not null)
        {
            List<int> vehicleCapacities = new List<int>();
            for (int i = 0; i < _numVehicles; i++)
                vehicleCapacities.Add(random.Next(50, 201));
            data["VehicleCapacities"] = vehicleCapacities.ToArray();
        }

        // Generate random time windows for customers (in minutes)
        if (_hasTimeWindows)
        {
            List<Tuple<int, int>> timeWindows = new List<Tuple<int, int>>();
            for (int i = 0; i < _numLocations; i++)
            {
                int startTime = random.Next(0, 301);
                int endTime = random.Next(301, 721);
                timeWindows.Add(new Tuple<int, int>(startTime, endTime));
            }
            data["TimeWindows"] = timeWindows.ToArray();
        }
        return data;
    }

    static void Main(string[] args)
    {
        var vrpInstance = new VRPDataGenerator(numLocations: int.Parse(args[1]), numVehicles: int.Parse(args[2]));
        var data = vrpInstance.GenerateData();
        // Serialize data to JSON format
        var settings = new JsonSerializerSettings
        {
            Converters = { new TupleConverter() },
            Formatting = Newtonsoft.Json.Formatting.Indented
        };

        string jsonData = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented, settings);

        if (args.Length > 0)
        {
            string filePath = args[0];
            // Save JSON data to a file
            File.WriteAllText(filePath, jsonData);
        }
    }
}