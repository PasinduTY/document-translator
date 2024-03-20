using Azure.Storage.Blobs;
using System.IO.Compression;

public class ResetService : IResetService
{
    private readonly string blobServiceClientEndpoint;
    private IConfiguration _configuration;

    public ResetService(IConfiguration configuration)
    {
        _configuration = configuration;
        blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");
    }
    public async Task DeleteKeyAndValueFolders(string operationGuid)
    {
        string keyFolderPath = @$"Temporary_documents\keys\{operationGuid}";
        string valuesFolderPath = @$"Temporary_documents\values\{operationGuid}";

        if (Directory.Exists(keyFolderPath))
        {
            await Task.Run(() => Directory.Delete(keyFolderPath, true));
            Console.WriteLine("Key folder deleted successfully.");
        }
        else
        {
            Console.WriteLine("Key folder does not exist.");
        }

        if (Directory.Exists(valuesFolderPath))
        {
            await Task.Run(() => Directory.Delete(valuesFolderPath, true));
            Console.WriteLine("Values folder deleted successfully.");
        }
        else
        {
            Console.WriteLine("Values folder does not exist.");
        }
    }

    public async Task DeleteTranslatedDocumentFolder(string operationGuid)
    {
        string translatedDocumentFolder = @$"Temporary_documents\translated_documents\{operationGuid}";

        if (Directory.Exists(translatedDocumentFolder))
        {
            await Task.Run(() => Directory.Delete(translatedDocumentFolder, true));
            Console.WriteLine("Key folder deleted successfully.");
        }
        else
        {
            Console.WriteLine("Key folder does not exist.");
        }
    }

    public async Task DeleteZipFolderInRoot(string operationGuid)
    {
        string zipFolderPath = @$"wwwroot\translated_files_as_zip\{operationGuid}";

        if (Directory.Exists(zipFolderPath))
        {
            await Task.Run(() => Directory.Delete(zipFolderPath, true));
            Console.WriteLine("Key folder deleted successfully.");
        }
        else
        {
            Console.WriteLine("Key folder does not exist.");
        }
    }

    public async Task DeleteFilesInInputContainerOfOperation(string operationGuid)
    {
        string containerName = "inputdocs";

        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }

    }

    public async Task DeleteFilesInOutputContainerOfOperation(string operationGuid)
    {
        string containerName = "translateddocs";

        var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: operationGuid))
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
            Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
        }
    }
}

