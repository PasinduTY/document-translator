
//using Azure.Core;
//using Newtonsoft.Json.Linq;
//using System.Collections;
//using System.IO;
//using static MimeDetective.Definitions.Default;

//public class SynchronousTranslationService : ISynchronousTranslationService
//{
//    private static readonly string endpoint = "https://abc-ai-translator.cognitiveservices.azure.com/";
//    private static readonly string subscriptionKey = "e40a0130bc4b4c34bb2fd3dd16fe2752";
//    private static readonly string apiVersion = "2023-11-01-preview";

//    public async Task<byte[]> TranslateDocument(byte[] inputDocument, string targetLanguage)
//    {
//        string url = $"{endpoint}/translator/document:translate";
//        using (HttpClient client = new HttpClient())
//        {
//            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

//            var content = new MultipartFormDataContent();

//            if (IdentifyJson(inputDocument))
//            {
                
//            }
//            content.Add(new ByteArrayContent(inputDocument), "document", "document-translation-sample.docx");
//            //MemoryStream stream = new MemoryStream(inputDocument);
//            //content.Add(new StreamContent(stream), "document", Path.GetFileName("document-translation-sample.docx"));
            
//            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
//            queryString["targetLanguage"] = targetLanguage;
//            queryString["api-version"] = apiVersion;

//            url += "?" + queryString.ToString();

//            var response = await client.PostAsync(url, content);

//            if (response.IsSuccessStatusCode)
//            {
//                Console.WriteLine("Synchronous Successful");
//                byte[] translatedFile = await response.Content.ReadAsByteArrayAsync();
                
//                File.WriteAllBytes("D:/Syn/Output.docx", translatedFile);
//                return translatedFile;

//            }
//            else
//            {
//                string errorMessage = await response.Content.ReadAsStringAsync();
//                Console.WriteLine($"Error: {response.ReasonPhrase}. Details: {errorMessage}");
//                return null;

//            }
//        }
//    }



//    public string GetFileType(byte[] fileBytes)
//    {
//        if (fileBytes.Length > 0)
//        {
//            // Check for specific byte patterns to identify different file types
//            if (fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x03 && fileBytes[3] == 0x04)
//            {
//                return ".docx"; // Word document
//            }
//            else if (fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
//            {
//                return ".txt"; // UTF-8 encoded text
//            }
//            else if (fileBytes[0] == 0x4D && fileBytes[1] == 0x53 && fileBytes[2] == 0x48)
//            {
//                return ".mhtml"; // MHTML document
//            }
//            else if ((fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x07 && fileBytes[3] == 0x08) || (fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x05 && fileBytes[3] == 0x06))
//            {
//                return ".pptx"; // PowerPoint document
//            }
//            else if (fileBytes[0] == 0xD0 && fileBytes[1] == 0xCF && fileBytes[2] == 0x11 && fileBytes[3] == 0xE0 && fileBytes[4] == 0xA1 && fileBytes[5] == 0xB1 && fileBytes[6] == 0x1A && fileBytes[7] == 0xE1)
//            {
//                return ".xls"; // Excel document
//            }
//            else if (fileBytes[0] == 0xD0 && fileBytes[1] == 0xCF && fileBytes[2] == 0x11 && fileBytes[3] == 0xE0)
//            {
//                return ".msg"; // Outlook message
//            }
//            else if (fileBytes[0] == 0x3C && fileBytes[1] == 0x3F && fileBytes[2] == 0x78 && fileBytes[3] == 0x6D)
//            {
//                return ".xlf"; // XLIFF document
//            }
//            else if (fileBytes[0] == 0x3C && fileBytes[1] == 0x78 && fileBytes[2] == 0x6C && fileBytes[3] == 0x69)
//            {
//                return ".xliff"; // XLIFF document (alternative)
//            }
//            else if (fileBytes[0] == 0x3C && fileBytes[1] == 0x78 && fileBytes[2] == 0x6C && fileBytes[3] == 0x69)
//            {
//                return ".xliff"; // XLIFF document (alternative)
//            }
//            // Add more checks for other file types as needed
//            // Default case if no specific pattern matches
//            return "Unknown File Type";
//        }
//        else
//        {
//            return "Empty File";
//        }

//    }


//    public bool IdentifyJson(byte[] fileBytes)
//    {
//        try
//        {
//            string jsonString = System.Text.Encoding.UTF8.GetString(fileBytes);
//            JToken.Parse(jsonString);
//            return true;
//        }
//        catch (Exception)
//        {
//            return false;
//        }
//    }



//}

