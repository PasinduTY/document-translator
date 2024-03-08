using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
public class Translator : ITranslator
{

    private static readonly string endpoint = "https://testcreative.cognitiveservices.azure.com/translator/text/batch/v1.1";

    static readonly string route = "/batches";

    private static readonly string key = "d7459b863ba14c74a1d0ae0cf699da63";

    static string sourceUrl = "https://creative12.blob.core.windows.net/inputdocs";
    static string targetUrl = "https://creative12.blob.core.windows.net/translateddocs";


    public async Task Translate(string languageCode)
    {
       
        using HttpClient client = new HttpClient();
        using HttpRequestMessage request = new HttpRequestMessage();
        {
              string json = $"{{\"inputs\": [{{\"source\": {{\"sourceUrl\": \"{sourceUrl}\", \"storageSource\": \"AzureBlob\",\"language\": \"en\"}}, \"targets\": [{{\"targetUrl\": \"{targetUrl}\", \"storageSource\": \"AzureBlob\",\"category\": \"general\",\"language\": \"{languageCode}\"}}]}}]}}";


            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(endpoint + route);
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Content = content;

            HttpResponseMessage response = await client.SendAsync(request);
            string result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                using HttpRequestMessage jobStatusRequest = new HttpRequestMessage();
                jobStatusRequest.Method = HttpMethod.Get;
               // jobStatusRequest.RequestUri = new Uri(response.Headers.("Operation-Location"));
                Console.WriteLine($"Status code: {response.StatusCode}");
                Console.WriteLine();
                Console.WriteLine($"Response Headers:");
                Console.WriteLine(response.Headers);
            }
            else
                Console.Write("Error");

        }

    }

}
