
public class SynchronousTranslationService : ISynchronousTranslationService
{
    
    private static readonly string endpoint = "https://abc-ai-translator.cognitiveservices.azure.com/";
    private static readonly string subscriptionKey = "e40a0130bc4b4c34bb2fd3dd16fe2752";
    //private static readonly string sourceLanguage = "en";
    //private static readonly string targetLanguage = "hi";
    private static readonly string apiVersion = "2023-11-01-preview";

    public SynchronousTranslationService(IConfiguration configuration, ILogger<ISynchronousTranslationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _endpoint = _configuration.GetConnectionString("Translator.Endpoint");
        _key = _configuration.GetConnectionString("Translator.Key");
    }
    public async Task TranslateDocument(string inputFilePath, string outputFilePath, string targetLanguage)
    {
        string url = $"{_endpoint}/translator/document:translate";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _key);

            var content = new MultipartFormDataContent();
            var fileStream = new FileStream(inputFilePath, FileMode.Open);

            content.Add(new StreamContent(fileStream), "document", Path.GetFileName(inputFilePath));

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            //queryString["sourceLanguage"] = sourceLanguage;
            queryString["targetLanguage"] = targetLanguage;
            queryString["api-version"] = apiVersion;

            url += "?" + queryString.ToString();

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                using (var outputDocument = new FileStream(outputFilePath, FileMode.Create))
                {
                    //Console.WriteLine(response);
                    //await response.Content.ReadAsByteArrayAsync();
                    await response.Content.CopyToAsync(outputDocument);
                    _logger.LogInformation("Translation successful.");
                }
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {response.ReasonPhrase}. Details: {errorMessage}");
                
            }
        }
    }


}

