using System.Text;
using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Aspose.Cells;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using Aspose.Cells.Drawing;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

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
    string keyguid;

    private static readonly string endpoint = "https://abc-ai-translator.cognitiveservices.azure.com/";
    private static readonly string subscriptionKey = "e40a0130bc4b4c34bb2fd3dd16fe2752";
    private static readonly string apiVersion = "2023-11-01-preview";

    private static readonly string key = "d7459b863ba14c74a1d0ae0cf699da63";
    private static readonly string textendpoint = "https://api.cognitive.microsofttranslator.com";
    private static readonly string location = "centralindia";

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


    /// <summary>
    /// Converts JSON data to Excel format and saves the generated Excel files to temporary directories.
    /// </summary>
    /// <param name="file">The JSON file to be converted.</param>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// The unique GUID assigned to the generated Excel files.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> or <paramref name="operationGuid"/> is null.</exception>
    public async Task<string> ConvertToExcelAsync(MemoryStream memoryStreamOfJsonFile, string operationGuid)
    {
        try
        {
            // Using StreamReader to read the content of the file
            using var reader = new StreamReader(memoryStreamOfJsonFile);
            string json = await reader.ReadToEndAsync();

            // Deserializing JSON content into a dictionary
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // Creating workbooks and worksheets
            using Workbook keysWorkbook = new Workbook();
            using Worksheet keysWorksheet = keysWorkbook.Worksheets[0];
            using  Workbook valuesWorkbook = new Workbook();
            using Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

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
            Console.WriteLine( keysFilePath );
            string valuesFilePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{guidString}.xlsx");

            keysWorkbook.Save(keysFilePath);
            valuesWorkbook.Save(valuesFilePath);

            // Returning the generated GUID
            return guidString;
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the conversion process
            _logger.LogError(ex, "An error occurred while converting to Excel");
            throw;
        }
    }

    /// <summary>
    /// Combines data from Excel files into a JSON format.
    /// </summary>
    /// <param name="memoryStreamOfTranslatedExcelFile">The memory stream containing the translated Excel file.</param>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <param name="guidOfValueExcel">The GUID assigned to the Excel file containing values.</param>
    /// <returns>
    /// A JSON string representing the combined data from the Excel files.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryStreamOfTranslatedExcelFile"/>, <paramref name="operationGuid"/>, or <paramref name="guidOfValueExcel"/> is null.</exception>
    public async Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel)
    {
        try
        {
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", operationGuid, $"{guidOfValueExcel}.xlsx");

            // Load keys workbook from file
            using Workbook keysWorkbook = new Workbook(keyFolderPath);
            using Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

            // Create a workbook from the MemoryStream
            using Workbook valuesWorkbook = new Workbook(memoryStreamOfTranslatedExcelFile);
            using Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

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
            // Log any errors that occur during the combination and conversion process
            _logger.LogError(ex, "An error occurred while combining Excel files and converting to JSON");
            throw;
        }
    }

    /// <summary>
    /// Creates folders for a specific operation.
    /// </summary>
    /// <param name="folderName">The name of the folder for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="folderName"/> is null.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during folder creation.</exception>
    public async Task CreateFolderForOperation(string folderName)
    {
        try
        {
            // Construct paths for key and value folders within the temporary_documents directory
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", folderName);
            string valueFolderPath = Path.Combine("Temporary_documents", "values", folderName);

            // Create the key and value folders
            Directory.CreateDirectory(keyFolderPath);
            Directory.CreateDirectory(valueFolderPath);

            // Log information about the successful folder creation
            _logger.LogInformation($"Folders created for operation {folderName}");
        }
        catch (Exception ex)
        {
            // Log the error and rethrow the exception
            _logger.LogError(ex, $"An error occurred while creating folders for operation {folderName}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a memory stream from the Value Excel file associated with a specific operation.
    /// </summary>
    /// <param name="operationGuid">The GUID of the operation.</param>
    /// <param name="uploadedDocumentGuid">The GUID of the uploaded document.</param>
    /// <returns>A memory stream containing the contents of the Value Excel file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> or <paramref name="uploadedDocumentGuid"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the Value Excel file does not exist.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while retrieving the memory stream.</exception>
    public async Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid)
    {
        try
        {
            // Construct the file path for the Value Excel file
            string filePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{uploadedDocumentGuid}.xlsx");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                // Log the error and throw a FileNotFoundException
                _logger.LogError($"File does not exist: {filePath}");
                throw new FileNotFoundException("Value Excel file not found.", filePath);
            }

            // Read the file contents into a byte array
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Create a memory stream from the byte array
            MemoryStream memoryStreamOfValueExcelFile = new MemoryStream(fileBytes);
            return memoryStreamOfValueExcelFile;
        }
        catch (Exception ex)
        {
            // Log the error and rethrow the exception
            _logger.LogError(ex, $"An error occurred while getting memory stream from Value Excel for operation {operationGuid}");
            throw;
        }
    }

    /// <summary>
    /// Initiates the translation process for a specified language and operation.
    /// </summary>
    /// <param name="languageCode">The language code for the translation.</param>
    /// <param name="operationGuid">The GUID of the operation.</param>
    /// <returns>Number of successful translations if the translation process completes successfully; otherwise, returns 0.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="languageCode"/> or <paramref name="operationGuid"/> is null.</exception>
    public async Task<short> TranslateAsync(string languageCode, string operationGuid)
    {
        try
        {
            // Instantiate a new HttpClient within a using statement to ensure proper disposal
            using HttpClient client = new HttpClient();

            // Create a new HTTP request message
            using HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_endpoint + route),
                Content = new StringContent(
                    $"{{\"inputs\": [{{\"source\": {{\"sourceUrl\": \"{_sourceUrl}\", \"filter\": {{\"prefix\": \"{operationGuid}/\"}}}}, \"targets\": [{{\"targetUrl\": \"{_targetUrl}\", \"language\": \"{languageCode}\"}}]}}]}}",
                    Encoding.UTF8,
                    "application/json")
            };

            // Add subscription key to request headers
            request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

            // Send the HTTP request and await the response
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Log information about the response status code
                _logger.LogInformation($"Status code: {response.StatusCode}");

                // Extract the job status URL from the response headers
                string jobStatusUrl = response.Headers.GetValues("Operation-Location").FirstOrDefault();

                // Check the status of the translation job
                return await CheckJobStatusAsync(jobStatusUrl, client);
            }
            else
            {
                // Log an error if the request was not successful
                _logger.LogError($"Error: {response}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during the translation process
            _logger.LogError($"Error: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Checks the status of a job asynchronously.
    /// </summary>
    /// <remarks>
    /// This method polls the job status URL until either the job succeeds or a timeout occurs.
    /// </remarks>
    /// <param name="jobStatusUrl">The URL for checking the job status.</param>
    /// <param name="client">The HttpClient instance used for sending requests.</param>
    /// <returns>The number of successful translations if the job succeeded; otherwise, returns 0.</returns>
    private async Task<short> CheckJobStatusAsync(string jobStatusUrl, HttpClient client)
    {
        // Record the start time for timeout calculation
        DateTime startTime = DateTime.Now;

        // Poll the job status URL until either the job succeeds or a timeout occurs
        while ((DateTime.Now - startTime) <= TimeSpan.FromMinutes(_timeoutPeriod))
        {
            // Create a new HTTP request message for checking job status
            using HttpRequestMessage jobStatusRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(jobStatusUrl)
            };

            // Add subscription key to request headers
            jobStatusRequest.Headers.Add("Ocp-Apim-Subscription-Key", _key);

            // Send the HTTP request and await the response
            HttpResponseMessage jobStatusRequestResponse = await client.SendAsync(jobStatusRequest);

            // Check if the request was successful
            if (jobStatusRequestResponse.IsSuccessStatusCode)
            {
                // Parse the response JSON to extract the job status
                using JsonDocument document = await JsonDocument.ParseAsync(await jobStatusRequestResponse.Content.ReadAsStreamAsync());
                var root = document.RootElement;
                string status = root.GetProperty("status").GetString();

                // Check the status of the job
                if (status == "ValidationFailed" || status == "Canceled" || status == "Cancelling" || status == "Failed")
                {
                    _logger.LogInformation($"Reason for failure of translation : {root.GetProperty("error")}");
                    return 0;
                }

                if (status == "Succeeded")
                {
                    short numberOfSuccessfulTranslations = root.GetProperty("summary").GetProperty("success").GetInt16();
                    return numberOfSuccessfulTranslations;
                }

                // Log information about the status 
                _logger.LogInformation($"Status: {status}");
            }

            // Delay for a specified interval before checking again
            await Task.Delay(_timeIntervalPeriod);
        }

        // Log a message indicating a timeout occurred
        _logger.LogInformation("Timeout");

        return 0; // Timeout occurred
    }


    /// <summary>
    /// Uploads a document to Blob storage asynchronously.
    /// </summary>
    /// <param name="memoryStreamOfDocument">The memory stream containing the document to be uploaded.</param>
    /// <param name="blobName">The name of the blob to be created.</param>
    /// <returns>
    /// True if the document was uploaded successfully; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryStreamOfDocument"/> or <paramref name="blobName"/> is null.</exception>
    public async Task<bool> UploadDocumentsAsync(MemoryStream memoryStreamOfDocument, string blobName)
    {
        try
        {
            // Create a BlobServiceClient instance using the provided Blob service client endpoint
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);

            // Get a reference to the Blob container where the document will be uploaded
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");

            // Upload the document to Blob storage
            await containerClient.UploadBlobAsync(blobName, memoryStreamOfDocument);

            memoryStreamOfDocument.Close();
            // Log information about the successful upload
            _logger.LogInformation($"Uploaded {blobName} successfully.");

            return true; // Document uploaded successfully
        }
        catch (Exception ex)
        {
            // Log an error if an exception occurs during the upload process
            _logger.LogError(ex, $"Error uploading {blobName} to Blob storage: {ex.Message}");

            return false; // Document upload failed
        }
    }


    /// <summary>
    /// Downloads converted files from Blob storage and performs additional processing.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DownloadConvertedFiles(string operationGuid)
    {
        try
        {
            // Create a BlobServiceClient instance using the provided Blob service client endpoint
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);

            // Get a reference to the Blob container where the converted files are stored
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("translateddocs");

            // Get a list of blobs in the container with the specified prefix
            var blobs = blobContainerClient.GetBlobsAsync(prefix: operationGuid);

            // Create a directory to store the downloaded files
            Directory.CreateDirectory(@$"Temporary_documents\translated_documents\{operationGuid}");

            // Iterate over each blob in the container
            await foreach (var blobItem in blobs)
            {
                try
                {
                    // Get a reference to the BlobClient for the current blob
                    BlobClient blobClient = blobContainerClient.GetBlobClient(blobItem.Name);

                    // Extract file information from the blob name
                    string blobName = blobItem.Name;
                    string[] parts = blobName.Split('/');
                    string fileName = parts[parts.Length - 1];
                    string secondPartOfTheBlobName = parts[1];

                    // Check if the blob represents an Excel file
                    if (secondPartOfTheBlobName != "json")
                    {
                        // Download the blob to the specified directory
                        blobClient.DownloadTo(@$"Temporary_documents\translated_documents\{operationGuid}\" + fileName);
                        _logger.LogInformation($"{fileName} is downloaded succesfully");
                    }
                    else
                    {
                        // Handle JSON blob by converting it to Excel format and saving it
                        string guidOfValueExcelWithExtension = parts[2];
                        string[] seperatedParts = guidOfValueExcelWithExtension.Split('.');
                        string guidOfValueExcel = seperatedParts[0];
                        using var memoryStreamOfTranslatedExcelFile = new MemoryStream();
                        blobClient.DownloadTo(memoryStreamOfTranslatedExcelFile);

                        // Convert the JSON content to Excel format
                        string convertedJson = await CombineExcelToJson(memoryStreamOfTranslatedExcelFile, operationGuid, guidOfValueExcel);

                        // Save the converted JSON to a file
                        File.WriteAllText(@$"Temporary_documents\translated_documents\{operationGuid}\{guidOfValueExcel}.json", convertedJson);
                        _logger.LogInformation($"{guidOfValueExcel} is downloaded succesfully");
                    }
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during the download process
                    _logger.LogError($"An error occurred downloading document: {ex.Message}");
                }
            }

            // Create a directory for storing zip files
            string folderPathForStoringZipFiles = @$"wwwroot\translated_files_as_zip\{operationGuid}";
            Directory.CreateDirectory(folderPathForStoringZipFiles);

            // Create a zip file from the downloaded documents
            await CreateZipFromTranslatedDocumentsAsync(operationGuid);
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the download process
            _logger.LogError($"An error occurred while downloading converted files: {ex.Message}");
            throw;
        }
    }



    /// <summary>
    /// Creates a zip file from the translated documents stored in a directory.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task CreateZipFromTranslatedDocumentsAsync(string operationGuid)
    {
        // Define the source folder path containing the translated documents
        string sourceFolderPath = Path.Combine("Temporary_documents", "translated_documents", operationGuid);

        // Define the destination path for the zip file
        string destinationZipPath = Path.Combine("wwwroot", "translated_files_as_zip", operationGuid, "Translated_Files.zip");

        try
        {
            // Create a zip file from the source folder
            ZipFile.CreateFromDirectory(sourceFolderPath, destinationZipPath);

            // Log information about the successful creation of the zip file
            _logger.LogInformation($"Zip file created successfully: {destinationZipPath}");
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the zip file creation process
            _logger.LogError($"An error occurred while creating the zip file: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// Deletes the key and value folders associated with a specific operation.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DeleteKeyAndValueFolders(string operationGuid)
    {
        try
        {
            // Define the paths for the key and value folders
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", operationGuid);
            string valuesFolderPath = Path.Combine("Temporary_documents", "values", operationGuid);

            // Delete the key folder if it exists
            if (Directory.Exists(keyFolderPath))
            {
                Directory.Delete(keyFolderPath, true);
                _logger.LogInformation("Key folder deleted successfully.");
            }
            else
            {
                _logger.LogError("Key folder does not exist.");
            }

            // Delete the value folder if it exists
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
            // Log any errors that occur during the deletion process
            _logger.LogError(ex, $"An error occurred while deleting key and value folders for operation {operationGuid}");
            throw;
        }
    }


    /// <summary>
    /// Deletes the folder containing translated documents for a specific operation.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DeleteTranslatedDocumentFolder(string operationGuid)
    {
        try
        {
            string translatedDocumentFolder = Path.Combine("Temporary_documents", "translated_documents", operationGuid);

            if (Directory.Exists(translatedDocumentFolder))
            {
                // Delete the translated document folder if it exists
                Directory.Delete(translatedDocumentFolder, true);
                _logger.LogInformation("Translated document folder deleted successfully.");
            }
            else
            {
                // Log if the translated document folder does not exist
                _logger.LogInformation("Translated document folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the deletion process
            _logger.LogError(ex, $"An error occurred while deleting translated document folder for operation {operationGuid}");
            throw;
        }
    }


    /// <summary>
    /// Deletes the zip folder associated with a specific operation from the root directory.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DeleteZipFolderInRoot(string operationGuid)
    {
        try
        {
            // Define the path for the zip folder
            string zipFolderPath = Path.Combine("wwwroot", "translated_files_as_zip", operationGuid);

            // Delete the zip folder if it exists
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
            // Log any errors that occur during the deletion process
            _logger.LogError(ex, $"An error occurred while deleting zip folder in root for operation {operationGuid}");
            throw;
        }
    }

    /// <summary>
    /// Deletes all files associated with a specific operation from the input container.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DeleteFilesInInputContainerOfOperation(string operationGuid)
    {
        try
        {
            // Define the name of the input container
            string containerName = "inputdocs";

            // Create a BlobServiceClient instance
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);

            // Get a reference to the input container
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Iterate over each blob in the container with the specified operation GUID prefix
            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
            {
                // Delete the blob
                await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);

                // Log deletion of the blob
                _logger.LogInformation($"Blob '{blobItem.Name}' in Input Container deleted.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the deletion process
            _logger.LogError(ex, $"An error occurred while deleting files in input container for operation {operationGuid}");
            throw;
        }
    }

    /// <summary>
    /// Deletes all files associated with a specific operation from the output container.
    /// </summary>
    /// <param name="operationGuid">The unique identifier for the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operationGuid"/> is null.</exception>
    public async Task DeleteFilesInOutputContainerOfOperation(string operationGuid)
    {
        try
        {
            // Define the name of the output container
            string containerName = "translateddocs";

            // Create a BlobServiceClient instance
            var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);

            // Get a reference to the output container
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Iterate over each blob in the container with the specified operation GUID prefix
            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
            {
                // Delete the blob
                await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);

                // Log deletion of the blob
                _logger.LogInformation($"Blob '{blobItem.Name}' in Output Container deleted.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the deletion process
            _logger.LogError(ex, $"An error occurred while deleting files in output container for operation {operationGuid}");
            throw;
        }
    }


    // Following two methods should only used for administration purposes. Permission should not be given to normal users.

    /// <summary>
    /// Cleans up the output container by deleting all blobs.
    /// </summary>
    /// <remarks>
    /// This method deletes all blobs in the output container named "translateddocs".
    /// </remarks>
    public async Task CleanOutputContainer()
    {
        string containerName = "translateddocs";

        var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Iterate through each blob in the output container
        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            // Delete the blob if it exists
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }
    }

    /// <summary>
    /// Cleans up the input container by deleting all blobs.
    /// </summary>
    /// <remarks>
    /// This method deletes all blobs in the input container named "inputdocs".
    /// </remarks>
    public async Task CleanInputContainer()
    {
        string containerName = "inputdocs";

        var blobServiceClient = new BlobServiceClient(_blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Iterate through each blob in the input container
        await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
        {
            // Delete the blob if it exists
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }
    }
















































   
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Translates a document synchronously(No need to upload document to a container) to a selected language.
    /// </summary>
    /// <param name="inputDocument">The document that need to be translate</param>
    /// <param name="targetLanguage">Expected Language of the translated document</param>
    /// <param name="filename">File name of the inputdocument with the extension eg:Mydocument.docx </param>
    /// <returns>
    /// Returns the translated document as a byte array.
    /// </returns>

    public async Task<byte[]> SyncTranslateDocument(byte[] inputDocument, string targetLanguage, string filename)
    {
        bool isJson;
        string syncOperationGuid = Guid.NewGuid().ToString();
        byte[] translatedFile;
        string url = $"{endpoint}/translator/document:translate";
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            isJson = IdentifyJson(inputDocument);
            var content = new MultipartFormDataContent();
            MemoryStream stream = new MemoryStream(inputDocument);
            

            if (isJson)
            {
                await CreateFolderForOperation(syncOperationGuid);
                string decomposedDocumentGuid = await ConvertToExcelAsync(stream, syncOperationGuid);
                keyguid = decomposedDocumentGuid;
                stream = await GetTheMemoryStreamFromValueExcel(syncOperationGuid, decomposedDocumentGuid);
                content.Add(new StreamContent(stream), "document", "jsonfile.xlsx");
            }
            else 
            {
                content.Add(new StreamContent(stream), "document", filename);
            }
            //content.Add(new ByteArrayContent(inputDocument), "document", "document-translation-sample.docx");

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["targetLanguage"] = targetLanguage;
            queryString["api-version"] = apiVersion;

            url += "?" + queryString.ToString();

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Synchronous Successful");
                translatedFile = await response.Content.ReadAsByteArrayAsync();
                
                if (isJson) 
                {
                    MemoryStream memoryStream = new MemoryStream(translatedFile);
                    string translatedJson = await CombineExcelToJson(memoryStream,syncOperationGuid,keyguid);
                    translatedFile = Encoding.UTF8.GetBytes(translatedJson);
                }
                
                File.WriteAllBytes("D:/Syn/Output.docx", translatedFile);
                return translatedFile;

            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {response.ReasonPhrase}. Details: {errorMessage}");
                return null;

            }
        }
    }


    /// <summary>
    /// Identify a file whether json or not. 
    /// </summary>
    /// <param name="fileBytes">The document that need to check whether it is a json or not</param>
    /// <returns>
    /// If the file is a json, returns true. Otherwise false.
    /// </returns>
    public bool IdentifyJson(byte[] fileBytes)
    {
        try
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(fileBytes);
            JToken.Parse(jsonString);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Not a Json file");
            return false;
        }
    }


    /// <summary>
    /// Translates a text to a selected language.
    /// </summary>
    /// <param name="textToTranslate">The text that need to be translate</param>
    /// <param name="targetLanguage">Expected Language of the translated text</param>
    /// <returns>
    /// Returns the translated text as a string.
    /// </returns>
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
            request.RequestUri = new Uri(textendpoint + route);
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
                    return (t.text);
                }
            }

            return "Translation not available";

        }
    }


}

