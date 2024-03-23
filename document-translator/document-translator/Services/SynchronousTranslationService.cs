
public class SynchronousTranslationService : ISynchronousTranslationService
{
    
    private static readonly string endpoint = "https://abc-ai-translator.cognitiveservices.azure.com/";
    private static readonly string subscriptionKey = "e40a0130bc4b4c34bb2fd3dd16fe2752";
    //private static readonly string sourceLanguage = "en";
    //private static readonly string targetLanguage = "hi";
    private static readonly string apiVersion = "2023-11-01-preview";

    public async Task TranslateDocument(string inputFilePath, string outputFilePath, String targetLanguage)
    {
        string url = $"{endpoint}/translator/document:translate";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

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
                    Console.WriteLine("Synchronous Successful");
                }
            }
            else
            {  
                Console.WriteLine($"Error: {response.ReasonPhrase}");
            }
        }
    }


}

