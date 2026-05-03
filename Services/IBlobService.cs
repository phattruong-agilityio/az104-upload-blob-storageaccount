using AzureBlobUploadApi.Models;

namespace AzureBlobUploadApi.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<List<BlobFileModel>> ListFilesAsync();
    }
}
