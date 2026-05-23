namespace AzureBlobUploadApi.Models
{
    public class UploadRequest
    {
        public string SasUri { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}