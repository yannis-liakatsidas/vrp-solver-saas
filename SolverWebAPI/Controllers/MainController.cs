using Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ORToolsSolver.VRP;

[Route("api/[controller]")]
[ApiController]
public class MainController : ControllerBase
{
    private const string serviceName = "Solver Core";
    private const string actionPerformed = "Solved the requested problem";


    private static string processedData = string.Empty;
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    [HttpPost("runjob")]
    public async Task<IActionResult> Post([FromBody] object jsonData)
    {
        object result;

        if (jsonData is not null)
        {
            if (semaphore.Wait(0))
            {
                try
                {
                    // Your existing post logic
                    processedData = jsonData.ToString();

                    if (string.IsNullOrEmpty(processedData))
                    {
                        return NotFound("Processed data not available");

                    }
                    else
                    {
                        if (JsonValidator.IsValidJson(processedData))
                        {
                            var problemData = JsonConvert.DeserializeObject<ProblemData>(processedData);
                            Console.WriteLine($"Received job {problemData.Metadata.Id}.");
                            result = SolveProblem(processedData, problemData.ProblemType);

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
                                ActionPerformed = actionPerformed,
                                JobId = problemData.Metadata.Id
                            };

                            var loggingData = JsonConvert.SerializeObject(newRecord);
                            await ApiFunctions.SendDataToAPIAsync(loggingData, "http://10.3.2.76:80/api/main/logging").ConfigureAwait(false);
                            //await ApiFunctions.SendDataToAPIAsync(loggingData, "http://localhost:60000/api/main/logging").ConfigureAwait(false); //replace with actual URL

                            Console.WriteLine($"{timestamp}: Job {problemData.Metadata.Id} done. Sending to Results Queue.");

                            return Ok(result);
                        }

                        else
                            return BadRequest("Data were not in valid JSON format.");
                    }
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            else
            {
                DateTime? initialTime = null;
                if (JsonValidator.IsValidJson(processedData))
                {
                    var problemData = JsonConvert.DeserializeObject<ProblemData>(processedData);
                    initialTime = problemData.Metadata.Timestamp;

                }
                var failedResult = new VRPSolver.ResultObject
                {
                    Duration = initialTime.HasValue ? (DateTime.Now - initialTime).ToString() : string.Empty,
                    Success = false,
                    Info = "Service unavailable",
                    Result = 0
                };

                return StatusCode(503, failedResult);
            }
        }
        else
            return NoContent();
    }

    //[HttpGet("status")]
    //public IActionResult GetStatus()
    //{
    //    if (semaphore.Wait(0))
    //        return Ok(Enums.ApiStatus.Free);
    //    else
    //        return Ok(Enums.ApiStatus.Busy);
    //}

    #region Helpers
    public object SolveProblem(string data, Enums.ProblemType problemType)
    {
        try
        {
            object result = new();

            //make it general depending on the type of problem
            switch (problemType)
            {
                case Enums.ProblemType.VRP:
                    var solver = new VRPSolver(data);
                    result = solver.SolveVehicleRoutingProblem();
                    break;

                case Enums.ProblemType.Knapsack:
                    break;

                case Enums.ProblemType.CpSat:
                    break;

                default:
                    break;
            }
            return result;
        }
        //continue with calling the OR_Tools_Solver
        // Set canPost to true, allowing another POST request

        catch (Exception e)
        {
            return BadRequest($"Process terminated abnormally due to error: {e.Message}");
        }
    }
    #endregion
}


//chatGPT question
//so in the post method, i want the system to basically wait until there is no test.txt file in the specified location. 
//    Can i achieve this asynchronously? I m thinking of a function where, 
//    it will temporalily "lock" this operation until the file is deleted (which is done on the get) or until a specified amount of time passes 
//    (for example, if 5 minutes have passed, it will kill the operation because of not available resources). how can i achieve it?
