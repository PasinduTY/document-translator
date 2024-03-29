using System.Text;
using System.Transactions;
using Newtonsoft.Json;
public class TextTranslateService : ITextTranslateService
{
    private static readonly string key = "d7459b863ba14c74a1d0ae0cf699da63";
    private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";
    private static readonly string location = "centralindia";

    public async Task<string> TextTranslator(string textToTranslate, string targetLanguage)
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
            request.RequestUri = new Uri(endpoint + route);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
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
                    return(t.text);
                }
            }

            return "Translation not available";
            
        }
    }

}





