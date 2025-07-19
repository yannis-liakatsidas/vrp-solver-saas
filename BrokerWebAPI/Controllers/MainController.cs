using System.IO;
using System.IO.Pipes;
using Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQns.Packages;
using static ORToolsSolver.VRP.VRPSolver;

[Route("api/[controller]")]
[ApiController]
public class MainController : ControllerBase
{
    private readonly RpcClient _rpcClient = RpcClient.GetOrCreateRpcClient;
    private static string processedData = string.Empty;
    private readonly string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "records.csv");
    private static readonly SemaphoreSlim semaphore = new(1, 1);

    [HttpPost("runjob")]
    public async Task<IActionResult> PostAsync([FromBody] object jsonData)
    {
        if (jsonData is null) return NoContent();

        try
        {
            processedData = jsonData.ToString();
            if (string.IsNullOrEmpty(processedData))
            {
                return NotFound("Processed data not available");
            }
            else
            {
                var problemData = JsonConvert.DeserializeObject<ProblemData>(processedData);
                Console.WriteLine($"Request {problemData.Metadata.Id} received.");
                var result = await PublishDataToBrokerAsync(problemData).ConfigureAwait(false);
                return StatusCode(200, JsonConvert.SerializeObject(result));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");

            return BadRequest(e.Message);
        }
    }

    public async Task<ResultObject> PublishDataToBrokerAsync(ProblemData data)
    {
        //---------------PUBLISH TO RABBITMQ SECTION-------------// inside the server ip, so localhost is valid
        var response = await _rpcClient.CallAsync(data).ConfigureAwait(false);

        if (response is null || string.IsNullOrEmpty(response.Result))
            return new ResultObject()
            {
                Success = false,
                Info = "No data found."
            };

        ResultObject result;
        try
        {
            result = JsonConvert.DeserializeObject<ResultObject>(response.Result);
        }
        catch (JsonSerializationException ex)
        {
            result = new ResultObject()
            {
                Success = false,
                Info = $"Deserialization failed: {ex.Message}"
            };
        }
        return result;
    }

    [HttpPost("logging")]
    public async Task<IActionResult> UpdateCsvFileAsync([FromBody] object records)
    {
        List<CsvData> existingRecords;
        object result = new();

        if (records is null) return NoContent();

        CsvData csvLine = JsonConvert.DeserializeObject<CsvData>(records.ToString());

        if (await semaphore.WaitAsync(-1).ConfigureAwait(false))
        {
            try
            {
                using (var fileStream = System.IO.File.Exists(csvFilePath) ? System.IO.File.Open(csvFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite) : System.IO.File.Create(csvFilePath))
                {
                    existingRecords = CsvData.LoadCsvFile(fileStream);
                    existingRecords.Add(csvLine);
                    CsvData.SaveCsvFile(csvFilePath, existingRecords);
                }

                result = StatusCode(200, existingRecords);
            }
            catch (Exception e)
            {
                result = BadRequest(e.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }


        return (IActionResult)result;
    }
}