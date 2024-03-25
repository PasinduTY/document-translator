using System.Text;
using System.Transactions;
using Newtonsoft.Json;
public class TextTranslateService : ITextTranslateService
{
    private readonly ILogger _logger;
    private IConfiguration _configuration;
    private readonly string _endpoint;
    private readonly string _key;
    private static readonly string location = "centralindia";

    public TextTranslateService(IConfiguration configuration, ILogger<ISynchronousTranslationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _endpoint = _configuration.GetConnectionString("Translator.Endpoint");
        _key = _configuration.GetConnectionString("Translator.Key");
    }

    public async Task TextTranslator(string textToTranslate, string targetLanguage)
    {
        // Input and output languages are defined as parameters.
        string route = $"/translate?api-version=3.0&to={targetLanguage}";
        //string textToTranslate = "I would really like to drive your car around the block a few times!";
        object[] body = new object[] { new { Text = textToTranslate } };
        var requestBody = JsonConvert.SerializeObject(body);

        using (var client = new HttpClient())
        using (var request = new HttpRequestMessage())
        {
            // Build the request.
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(_endpoint + route);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _key);
            // location required if you're using a multi-service or regional (not global) resource.
            request.Headers.Add("Ocp-Apim-Subscription-Region", location);

            // Send the request and get response.
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            // Read response as a string.
            string result = await response.Content.ReadAsStringAsync();
            var translations = JsonConvert.DeserializeObject<dynamic[]>(result);

            // Iterate over translations and print "text" values
            foreach (var translation in translations)
            {
                foreach (var t in translation.translations)
                {
                    Console.WriteLine(t.text);
                }
            }
        }
    }

}





