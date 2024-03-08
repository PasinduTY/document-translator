
using Azure.Storage.Blobs;

    public class Uploader : IUploader
    {
    private IConfiguration _configuration;
    public Uploader(IConfiguration configuration) {
        _configuration = configuration;
    }
    
        public async Task Upload()
        {
            try
            {
            string blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");
            

            var blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);

                // Replace the localFilePath with the actual file path you want to upload
                string localFilePath = @"D:/Files for testing/Test.xlsx";
                string fileName = Path.GetFileName(localFilePath);
                Console.WriteLine(fileName);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("inputdocs");
                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

                await blobClient.UploadAsync(localFilePath, true);

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading to Blob storage: {ex.Message}");
              
            }
        }

      

    }

