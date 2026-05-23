using AzureBlobUploadApi.Models;

namespace AzureBlobUploadApi.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(IFormFile file, Uri containerSasUri);
        Task<List<BlobFileModel>> ListFilesAsync();
        Task<Uri> GenerateContainerSasUriAsync();
    }
}
