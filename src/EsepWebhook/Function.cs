using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        /// <summary>
        /// Function that receives GitHub webhook data, extracts the issue URL, and posts it to Slack.
        /// </summary>
        /// <param name="input">The GitHub webhook payload in JSON format.</param>
        /// <param name="context">The Lambda context for logging and environment details.</param>
        /// <returns>The response from Slack or an error message.</returns>
        public string FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogInformation($"FunctionHandler received: {input}");

            try
            {
                dynamic ?json = JsonConvert.DeserializeObject<dynamic>(input?.ToString() ?? string.Empty);
                 string url = json?.issue?.html_url?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(url))
                {
                    return "Error: No issue URL found in the payload.";
                }

                context.Logger.LogInformation($"Issue URL: {url}");

                string payload = $"{{\"text\":\"Issue Created: {url}\"}}";

                using (var client = new HttpClient())
                {
                    var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var response = client.Send(webRequest);
                    response.EnsureSuccessStatusCode(); 
                    using var reader = new StreamReader(response.Content.ReadAsStream());
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing GitHub webhook: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}