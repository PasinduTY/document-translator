using System.Text;
using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Aspose.Cells;
public class TranslatorService : ITranslatorService
{
    private IConfiguration _configuration;
    private readonly string endpoint;
    private readonly string sourceUrl;
    private readonly string targetUrl;
    private readonly string key;
    private readonly string blobServiceClientEndpoint;
    private readonly string timeoutPeriodAsString;
    private readonly string timeIntervalPeriodAsString;
    private readonly int timeoutPeriod;
    private readonly int timeIntervalPeriod;
    static readonly string route = "/batches";

    public TranslatorService(IConfiguration configuration)
    {
        _configuration = configuration;
        endpoint = _configuration.GetConnectionString("Translator.Endpoint");
        sourceUrl = _configuration.GetConnectionString("Input.Container.Url");
        targetUrl = _configuration.GetConnectionString("Output.Container.Url");
        key = _configuration.GetConnectionString("Translator.Key");
        blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");
        timeoutPeriodAsString = _configuration.GetConnectionString("Timeout.Period");
        timeIntervalPeriodAsString = _configuration.GetConnectionString("Time.Interval.Period");
        timeoutPeriod = int.Parse(timeoutPeriodAsString);
        timeIntervalPeriod = int.Parse(timeIntervalPeriodAsString);
    }

    public async Task<bool> Translate(string languageCode, string blobNameOfUploadedDocument)
    {

        using HttpClient client = new HttpClient();
        using HttpRequestMessage request = new HttpRequestMessage();
        {
            string sourceDocumentUrl = $"{sourceUrl}/{blobNameOfUploadedDocument}";
            string targetDocumentUrl = $"{targetUrl}/{blobNameOfUploadedDocument}";

            string json = $"{{\"inputs\": [{{\"storageType\": \"File\"," +
              $"\"source\": {{\"sourceUrl\": \"{sourceDocumentUrl}\"}}," +
              $"\"targets\": [{{\"targetUrl\": \"{targetDocumentUrl}\", \"language\": \"{languageCode}\"}}]}}]}}";

           // string json = $"{{\"inputs\": [{{\"source\": {{\"sourceUrl\": \"{sourceUrl}\", \"storageSource\": \"AzureBlob\",\"language\": \"en\"}}, \"targets\": [{{\"targetUrl\": \"{targetUrl}\", \"storageSource\": \"AzureBlob\",\"category\": \"general\",\"language\": \"{languageCode}\"}}]}}]}}";

            Console.WriteLine("GGGGGG");

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(endpoint + route);
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Content = content;

            HttpResponseMessage response = await client.SendAsync(request);

            string result = response.Content.ReadAsStringAsync().Result;
            string jobStatusUrl = response.Headers.GetValues("Operation-Location").FirstOrDefault();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Status code: {response.StatusCode}");
                Console.WriteLine();
                Console.WriteLine($"Response Headers:");
                Console.WriteLine(response.Headers);

                return await CheckJobStatusAsync(jobStatusUrl, client, key, timeoutPeriod, timeIntervalPeriod);

            }
            else
            {
                Console.Write("Error");
                return false;
            }
        }

    }

    private async Task<bool> CheckJobStatusAsync(string jobStatusUrl, HttpClient client, string key, int timeoutPeriod, int timeIntervalPeriod)
    {
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime) <= TimeSpan.FromMinutes(timeoutPeriod))
        {
            using HttpRequestMessage jobStatusRequest = new HttpRequestMessage();
            jobStatusRequest.Method = HttpMethod.Get;
            jobStatusRequest.RequestUri = new Uri(jobStatusUrl);
            jobStatusRequest.Headers.Add("Ocp-Apim-Subscription-Key", key);
            HttpResponseMessage jobStatusRequestResponse = await client.SendAsync(jobStatusRequest);

            if (jobStatusRequestResponse.StatusCode == HttpStatusCode.OK)
            {
                using (JsonDocument document = await JsonDocument.ParseAsync(await jobStatusRequestResponse.Content.ReadAsStreamAsync()))
                {
                    var root = document.RootElement;
                    string status = root.GetProperty("status").GetString();
                    if (status == "ValidationFailed")
                    {
                        return false;
                    }
                    if (status == "Succeeded")
                    {
                        return true;
                    }
                    Console.WriteLine($"Status code: {jobStatusRequestResponse.StatusCode}");
                    Console.WriteLine($"Status: {status}");
                }
            }
            Console.WriteLine("Here we go");

            await Task.Delay(timeIntervalPeriod);
        }
        Console.WriteLine("Timeout");
        return false;
    }

    //public async Task<bool> Upload(IBrowserFile file, string fileName)
    //{
    //    try
    //    {
    //        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
    //        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");
    //        Guid guid = Guid.NewGuid();
    //        string blobName = $"{guid}/{fileName}";
    //        await containerClient.UploadBlobAsync(blobName, file.OpenReadStream());
    //        // BlobClient blobClient = containerClient.GetBlobClient(fileName);
    //        Console.WriteLine("Uploaded" + fileName);
    //        return true;
    //        //await blobClient.UploadAsync(localFilePath, true);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error uploading to Blob storage: {ex.Message}");
    //        return false;
    //    }
    //}
    public async Task<bool> Upload(MemoryStream memoryStreamOfDocument, string guideAsFileName)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");
            Guid guid = Guid.NewGuid();
            string blobName = $"{guideAsFileName}/{guideAsFileName}.xlsx";
            await containerClient.UploadBlobAsync(blobName, memoryStreamOfDocument);
            // BlobClient blobClient = containerClient.GetBlobClient(fileName);
            Console.WriteLine("Uploaded" + guideAsFileName);
            return true;
            //await blobClient.UploadAsync(localFilePath, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading to Blob storage: {ex.Message}");
            return false;
        }
    }


    public async Task<string> Upload(Workbook file)
    {
        try
        {
            string fileName = $"{file.FileName}.xlsx";
            var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);

            var stream = new MemoryStream();

            // Save the workbook to the MemoryStream
            file.Save(stream, SaveFormat.Xlsx);

            // Reset the stream position to the beginning
            stream.Position = 0;

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");
            Guid guid = Guid.NewGuid();
            string blobName = $"{guid}/{fileName}";
            await containerClient.UploadBlobAsync(blobName, stream);

            Console.WriteLine("Uploaded" + blobName);
            return blobName;
            //await blobClient.UploadAsync(localFilePath, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading to Blob storage: {ex.Message}");
            return "";
        }
    }

    public async Task CleanInputContainer()
    {

        string containerName = "inputdocs";

        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }

    }

    public async Task DownloadeConvertedFiles()
    {
        string destinationFolder = @"C:\Users\vibuda.S\Downloads\";

        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("translateddocs");

        var blobs = blobContainerClient.GetBlobs();

        foreach (var blobItem in blobs)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
            blobClient.DownloadTo(destinationFolder + blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' downloaded.");
        }

        // return Task.CompletedTask;
    }

    public async Task CleanOutputContainer()
    {
        string containerName = "translateddocs";

        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }
    }


}

