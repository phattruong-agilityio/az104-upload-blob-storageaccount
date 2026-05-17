using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureBlobUploadApi.Models;

namespace AzureBlobUploadApi.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("AzureStorageBlobEndpoint");
            var blobServiceSASToken = config.GetConnectionString("AzureBlobServiceSASToken");
            var containerName = config["AzureBlob:ContainerName"];
            var blobServiceClient = new BlobServiceClient(new Uri(connectionString + blobServiceSASToken));
            
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        // Upload file to container
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            await _containerClient.CreateIfNotExistsAsync();

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = _containerClient.GetBlobClient(fileName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true); // overwrite = true

            return blobClient.Uri.ToString();
        }

        // List all files in the container
        public async Task<List<BlobFileModel>> ListFilesAsync()
        {
            var files = new List<BlobFileModel>();
            
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                BlobClient blobClient = _containerClient.GetBlobClient(blobItem.Name);

                files.Add(new BlobFileModel()
                {
                    FileName = blobItem.Name,
                    CreatedOn = blobItem.Properties.CreatedOn,
                    Url = blobClient.Uri.ToString()
                });
            }

            return files;
        }
    }
}
