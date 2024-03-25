using System.Text;
using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Aspose.Cells;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
public class TranslatorService : ITranslatorService
{
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

    public TranslatorService(IConfiguration configuration, ILogger<ITranslatorService> logger)
    {
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


    public async Task<string> ConvertToExcelAsync(IBrowserFile file, string operationGuid)
    {
        try
        {
            // Using MemoryStream and StreamReader to read the content of the file
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(memoryStream);
            string json = await reader.ReadToEndAsync();

            // Deserializing JSON content into a dictionary
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // Creating workbooks and worksheets
            Workbook keysWorkbook = new Workbook();
            Worksheet keysWorksheet = keysWorkbook.Worksheets[0];
            Workbook valuesWorkbook = new Workbook();
            Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

            // Populating worksheets with key-value pairs
            int row = 0;
            foreach (var kvp in jsonObject)
            {
                keysWorksheet.Cells[row, 0].PutValue(kvp.Key);
                valuesWorksheet.Cells[row, 0].PutValue(kvp.Value);
                row++;
            }

            // Generating a unique GUID for filenames
            Guid guid = Guid.NewGuid();
            string guidString = guid.ToString();

            // Setting filenames for workbooks
            keysWorkbook.FileName = guidString;
            valuesWorkbook.FileName = guidString;

            // Saving workbooks to temporary directories
            string keysFilePath = Path.Combine("Temporary_documents", "keys", operationGuid, $"{guidString}.xlsx");
            string valuesFilePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{guidString}.xlsx");

            keysWorkbook.Save(keysFilePath);
            valuesWorkbook.Save(valuesFilePath);

            // Returning the generated GUID
            return guidString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while converting to Excel");
            throw;
        }
    }

    public async Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel)
    {
        try
        {
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", operationGuid, $"{guidOfValueExcel}.xlsx");

            // Load keys workbook from file
            Workbook keysWorkbook = new Workbook(keyFolderPath);
            Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

            // Create a workbook from the MemoryStream
            Workbook valuesWorkbook = new Workbook(memoryStreamOfTranslatedExcelFile);
            Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

            Dictionary<string, string> combinedData = new Dictionary<string, string>();

            // Iterate through each row in the keys worksheet
            for (int i = 0; i <= keysWorksheet.Cells.MaxDataRow; i++)
            {
                string key = keysWorksheet.Cells[i, 0].Value.ToString();
                string value = valuesWorksheet.Cells[i, 0].Value.ToString();

                combinedData.Add(key, value);
            }

            // Serialize combined data to JSON with indentation
            string jsonOutput = JsonConvert.SerializeObject(combinedData, Formatting.Indented);

            return jsonOutput;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while combine excels and convert to json");
            throw;
        }
    }
    public async Task CreateFolderForOperation(string folderName)
    {
        try
        {
              string keyFolderPath = Path.Combine("Temporary_documents", "keys", folderName);
              string valueFolderPath = Path.Combine("Temporary_documents", "values", folderName);

              Directory.CreateDirectory(keyFolderPath);
              Directory.CreateDirectory(valueFolderPath);
              _logger.LogInformation($"Folders created for operation {folderName}"); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while creating folders for operation {folderName}");
            throw;
        }
    }

    public async Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid)
    {
        try
        {
            string filePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{uploadedDocumentGuid}.xlsx");

            if (!File.Exists(filePath))
            {
                _logger.LogError($"File does not exist: {filePath}");
                throw new FileNotFoundException("Value Excel file not found.", filePath);
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            MemoryStream memoryStreamOfValueExcelFile = new MemoryStream(fileBytes);
            return memoryStreamOfValueExcelFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while getting memory stream from Value Excel for operation {operationGuid}");
            throw;
        }
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
        try
        {
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("translateddocs");

            var blobs = blobContainerClient.GetBlobsAsync(prefix: operationGuid);

            Directory.CreateDirectory(@$"Temporary_documents\translated_documents\{operationGuid}");

            await foreach (var blobItem in blobs)
            {
                try
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
                        string consvertedJson = await CombineExcelToJson(memoryStreamOfTranslatedExcelFile, operationGuid, guidOfValueExcel);
                        File.WriteAllText(@$"Temporary_documents\translated_documents\{operationGuid}\{guidOfValueExcel}.json", consvertedJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred downloading document: {ex.Message}");
                }
            }

            string folderPathForStoringZipFiles = @$"wwwroot\translated_files_as_zip\{operationGuid}";
            Directory.CreateDirectory(folderPathForStoringZipFiles);
            await CreateZipFromTranslatedDocumentsAsync(operationGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while downloading converted files{ex.Message}");
            throw;
        }
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
             throw;
        }
    }

    public async Task DeleteKeyAndValueFolders(string operationGuid)
    {
        try
        {
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", operationGuid);
            string valuesFolderPath = Path.Combine("Temporary_documents", "values", operationGuid);

            if (Directory.Exists(keyFolderPath))
            {
                Directory.Delete(keyFolderPath, true);
                _logger.LogInformation("Key folder deleted successfully.");
            }
            else
            {
                _logger.LogError("Key folder does not exist.");
            }

            if (Directory.Exists(valuesFolderPath))
            {
                Directory.Delete(valuesFolderPath, true);
                _logger.LogInformation("Values folder deleted successfully.");
            }
            else
            {
                _logger.LogError("Values folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting key and value folders for operation {operationGuid}");
            throw;
        }
    }

    public async Task DeleteTranslatedDocumentFolder(string operationGuid)
    {
        try
        {
            string translatedDocumentFolder = Path.Combine("Temporary_documents", "translated_documents", operationGuid);

            if (Directory.Exists(translatedDocumentFolder))
            {
                Directory.Delete(translatedDocumentFolder, true);
                _logger.LogInformation("Translated document folder deleted successfully.");
            }
            else
            {
                _logger.LogInformation("Translated document folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting translated document folder for operation {operationGuid}");
            throw;
        }
    }

    public async Task DeleteZipFolderInRoot(string operationGuid)
    {
        try
        {
            string zipFolderPath = Path.Combine("wwwroot", "translated_files_as_zip", operationGuid);

            if (Directory.Exists(zipFolderPath))
            {
                Directory.Delete(zipFolderPath, true);
                _logger.LogInformation("Zip folder deleted successfully.");
            }
            else
            {
                _logger.LogInformation("Zip folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting zip folder in root for operation {operationGuid}");
            throw;
        }
    }

    public async Task DeleteFilesInInputContainerOfOperation(string operationGuid)
    {
        try
        {
            string containerName = "inputdocs";

            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
            {
                await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
                _logger.LogInformation($"Blob '{blobItem.Name}' in Input Container deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting files in input container for operation {operationGuid}");
            throw;
        }
    }

    public async Task DeleteFilesInOutputContainerOfOperation(string operationGuid)
    {
        try
        {
            string containerName = "translateddocs";

            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
            {
                await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
                _logger.LogInformation($"Blob '{blobItem.Name}' in Output Container deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting files in output container for operation {operationGuid}");
            throw;
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

