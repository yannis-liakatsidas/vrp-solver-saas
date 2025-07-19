using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Helpers.Enums;

namespace Helpers
{
    public interface IJobData
    {
        //maybe add an interface?
    }
    public class Metadata
    {
        public required int Id { get; set; }
        public required DateTime Timestamp { get; set; }
    }

    public class ProblemData
    {
        public required Dictionary<string, object> JobData { get; set; }
        public required Metadata Metadata { get; set; }
        public required ProblemType ProblemType { get; set; }
    }

    public class JsonValidator
    {
        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith('{') && strInput.EndsWith('}')) || //For object
                (strInput.StartsWith('[') && strInput.EndsWith(']'))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
