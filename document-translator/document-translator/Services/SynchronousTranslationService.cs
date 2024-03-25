
using Microsoft.Extensions.Configuration;

public class SynchronousTranslationService : ISynchronousTranslationService
{
    
    private readonly ILogger _logger;
    private IConfiguration _configuration;
    private readonly string _endpoint;
    private readonly string _key;
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
                    await response.Content.CopyToAsync(outputDocument);
                    _logger.LogInformation("Translation successful.");
                }
            }
            else
            {
                _logger.LogError("Error during translation");
            }
        }
    }


}

