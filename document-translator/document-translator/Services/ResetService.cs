using Azure.Storage.Blobs;
using System.IO.Compression;

public class ResetService : IResetService
{
    private readonly string _blobServiceClientEndpoint;
    private IConfiguration _configuration;
    private readonly ILogger<ITranslatorService> _logger;

    public ResetService(IConfiguration configuration, ILogger<ITranslatorService> logger)
    {
        _configuration = configuration;
        _blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");
        _logger = logger;
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
                _logger.LogInformation($"Blob '{blobItem.Name}' deleted.");
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
                _logger.LogInformation($"Blob '{blobItem.Name}' deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while deleting files in output container for operation {operationGuid}");
            throw; 
        }
    }
}

