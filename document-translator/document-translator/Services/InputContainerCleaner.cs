using Azure.Storage.Blobs;
using System;
using System.Threading.Tasks;
public class InputContainerCleaner
{
    public async Task CleanInputContainer()
    {
       
            string connectionString = "your_storage_account_connection_string";
            string containerName = "your_container_name";
        
            var blobServiceClient = new BlobServiceClient(connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
            {
                await blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name);
                Console.WriteLine($"Blob '{blobItem.Name}' deleted.");
            }
        
    }

}

