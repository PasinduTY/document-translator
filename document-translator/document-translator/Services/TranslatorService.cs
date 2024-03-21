using System.Text;
using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Aspose.Cells;
using System;
using System.IO.Compression;
public class TranslatorService : ITranslatorService
{
    private IConverterService _converterService;
    private IConfiguration _configuration;
    private readonly ILogger<ITranslatorService> _logger;
    private readonly string _endpoint;
    private readonly string _sourceUrl;
    private readonly string _targetUrl;
    private readonly string _key;
    private readonly string _blobServiceClientEndpoint;
    private readonly string _timeoutPeriodAsString;
    private readonly string _timeIntervalPeriodAsString;
    private readonly int _timeoutPeriod;
    private readonly int _timeIntervalPeriod;
    static readonly string route = "/batches";

    public TranslatorService(IConfiguration configuration, IConverterService converterService, ILogger<ITranslatorService> logger)
    {
        _converterService = converterService;
        _configuration = configuration;
        _endpoint = _configuration.GetConnectionString("Translator.Endpoint");
        _sourceUrl = _configuration.GetConnectionString("Input.Container.Url");
        _targetUrl = _configuration.GetConnectionString("Output.Container.Url");
        _key = _configuration.GetConnectionString("Translator.Key");
        _blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");
        _timeoutPeriodAsString = _configuration.GetConnectionString("Timeout.Period");
        _timeIntervalPeriodAsString = _configuration.GetConnectionString("Time.Interval.Period");
        _timeoutPeriod = int.Parse(_timeoutPeriodAsString);
        _timeIntervalPeriod = int.Parse(_timeIntervalPeriodAsString);
        _logger = logger;
    }

    public async Task<bool> TranslateAsync(string languageCode, string operationGuid)
    {
        using HttpClient client = new HttpClient();
        using HttpRequestMessage request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_endpoint + route),
            Content = new StringContent(
                $"{{\"inputs\": [{{\"source\": {{\"sourceUrl\": \"{_sourceUrl}\", \"filter\": {{\"prefix\": \"{operationGuid}/\"}}}}, \"targets\": [{{\"targetUrl\": \"{_targetUrl}\", \"language\": \"{languageCode}\"}}]}}]}}",
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Status code: {response.StatusCode}");
            _logger.LogInformation("Response Headers:");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation($"{header.Key}: {string.Join(",", header.Value)}");
            }

            string jobStatusUrl = response.Headers.GetValues("Operation-Location").FirstOrDefault();
            return await CheckJobStatusAsync(jobStatusUrl, client);
        }
        else
        {
            _logger.LogError($"Error: {response.StatusCode}");
            return false;
        }
    }


    private async Task<bool> CheckJobStatusAsync(string jobStatusUrl, HttpClient client)
    {
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime) <= TimeSpan.FromMinutes(_timeoutPeriod))
        {
            using HttpRequestMessage jobStatusRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(jobStatusUrl)
            };

            jobStatusRequest.Headers.Add("Ocp-Apim-Subscription-Key", _key);

            HttpResponseMessage jobStatusRequestResponse = await client.SendAsync(jobStatusRequest);

            if (jobStatusRequestResponse.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument document = await JsonDocument.ParseAsync(await jobStatusRequestResponse.Content.ReadAsStreamAsync());
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

                _logger.LogInformation($"Status code: {jobStatusRequestResponse.StatusCode}");
                _logger.LogInformation($"Status: {status}");
            }
            await Task.Delay(_timeIntervalPeriod);
        }
        _logger.LogInformation("Timeout");
        return false;
    }

    public async Task<bool> UploadDocumentsAsync(MemoryStream memoryStreamOfDocument, string blobName)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");
            await containerClient.UploadBlobAsync(blobName, memoryStreamOfDocument);

            _logger.LogInformation($"Uploaded {blobName} successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading {blobName} to Blob storage: {ex.Message}");
            return false;
        }
    }

    public async Task DownloadConvertedFiles(string operationGuid)
    {
        var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("translateddocs");

        var blobs = blobContainerClient.GetBlobsAsync(prefix: operationGuid);

        Directory.CreateDirectory(@$"Temporary_documents\translated_documents\{operationGuid}");

        await foreach (var blobItem in blobs)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
            string blobName = blobItem.Name;
            string[] parts = blobName.Split('/');
            string fileName = parts[parts.Length - 1];
            string secondPartOfTheBlobName = parts[1];
            if (secondPartOfTheBlobName != "json")
            {
                blobClient.DownloadTo(@$"Temporary_documents\translated_documents\{operationGuid}\" + fileName);
                Console.WriteLine($"Blob '{blobItem.Name}' downloaded.");
            }
            else
            {
                string guidOfValueExcelWithExtension = parts[2];
                string[] seperatedParts = guidOfValueExcelWithExtension.Split('.');
                string guidOfValueExcel = seperatedParts[0];
                var memoryStreamOfTranslatedExcelFile = new MemoryStream();
                blobClient.DownloadTo(memoryStreamOfTranslatedExcelFile);
                // Create a workbook from the MemoryStream
                // Workbook translatedValuesWorkbook = new Workbook(memoryStreamOfTranslatedExcelFile);
                string consvertedJson = await _converterService.CombineExcelToJson(memoryStreamOfTranslatedExcelFile, operationGuid, guidOfValueExcel);
                File.WriteAllText(@$"Temporary_documents\translated_documents\{operationGuid}\{guidOfValueExcel}.json", consvertedJson);
            }
        }

        string folderPathForStoringZipFiles = @$"wwwroot\translated_files_as_zip\{operationGuid}";
        Directory.CreateDirectory(folderPathForStoringZipFiles);
        CreateZipFromTranslatedDocumentsAsync(operationGuid);
        // return Task.CompletedTask;
    }

    public async Task CreateZipFromTranslatedDocumentsAsync(string operationGuid)
    {
        string sourceFolderPath = Path.Combine("Temporary_documents", "translated_documents", operationGuid);
        string destinationZipPath = Path.Combine("wwwroot", "translated_files_as_zip", operationGuid, "Translated_Files.zip");

        try
        {
            ZipFile.CreateFromDirectory(sourceFolderPath, destinationZipPath);
            _logger.LogInformation($"Zip file created successfully: {destinationZipPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while creating the zip file: {ex.Message}");
            // throw;
        }
    }
    /*******************************************************************************************************/
    public async Task CleanOutputContainer()
    {
        string containerName = "translateddocs";

        var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }
    }

    public async Task CleanInputContainer()
    {

        string containerName = "inputdocs";

        var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }

    }
}

