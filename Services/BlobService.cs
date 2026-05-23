using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureBlobUploadApi.Models;

namespace AzureBlobUploadApi.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;

        public BlobService(IConfiguration config)
        {
            var serviceUri = config["AzureBlob:ServiceUri"]
                ?? throw new InvalidOperationException("AzureBlob:ServiceUri is not configured.");
            var containerName = config["AzureBlob:ContainerName"]
                ?? throw new InvalidOperationException("AzureBlob:ContainerName is not configured.");

            // Use DefaultAzureCredential to automatically pick up az login or Managed Identity.
            _blobServiceClient = new BlobServiceClient(
                new Uri(serviceUri),
                new DefaultAzureCredential());

            _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadFileAsync(IFormFile file, Uri containerSasUri)
        {
            try 
            {
                // Create a container client from SAS URI (does not require Entra credentials).
                var containerClientSas = new BlobContainerClient(containerSasUri);

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var blobClient = containerClientSas.GetBlobClient(fileName);

                await using var stream = file.OpenReadStream();

                await blobClient.UploadAsync(stream);

                return blobClient.Uri.ToString();
            }
            catch (Azure.RequestFailedException ex)
            {
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
            
        }

        public async Task<List<BlobFileModel>> ListFilesAsync()
        {
            var files = new List<BlobFileModel>();

            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                BlobClient blobClient = _containerClient.GetBlobClient(blobItem.Name);
                files.Add(new BlobFileModel
                {
                    FileName = blobItem.Name,
                    CreatedOn = blobItem.Properties.CreatedOn,
                    Url = blobClient.Uri.ToString()
                });
            }

            return files;
        }

        public async Task<Uri> GenerateContainerSasUriAsync()
        {
            // Use the same time window for key and SAS to reduce clock-skew authorization issues.
            var keyStartsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
            var keyExpiresOn = DateTimeOffset.UtcNow.AddDays(1);

            // Request a User Delegation Key from Azure for one day.
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                keyStartsOn,
                keyExpiresOn);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                Resource = "c",                      // "c" = container scope
                StartsOn = keyStartsOn,
                ExpiresOn = keyExpiresOn
            };

            // Grant minimal permissions for the current upload/list flow.
            sasBuilder.SetPermissions(
                BlobSasPermissions.Read |
                BlobSasPermissions.Write |
                BlobSasPermissions.Create |
                BlobSasPermissions.Add |
                BlobSasPermissions.List);

            // Sign the SAS with the User Delegation Key (without Account Key).
            var uriBuilder = new BlobUriBuilder(_containerClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(
                    userDelegationKey,
                    _blobServiceClient.AccountName)
            };

            return uriBuilder.ToUri();
        }
    }
}
