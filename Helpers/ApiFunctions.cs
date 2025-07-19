using System.Text;

namespace Helpers
{
    public class ApiFunctions
    {
        public static async Task<HttpResponseMessage> SendDataToAPIAsync(string jsonData, string apiUrl)
        {
            using HttpClient client = new HttpClient()
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Set the content type header if needed
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Send the POST request
            HttpResponseMessage response = await client.PostAsync(apiUrl, content).ConfigureAwait(false);
            return response;
        }
    }
}
