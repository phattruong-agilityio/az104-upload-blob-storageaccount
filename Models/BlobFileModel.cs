namespace AzureBlobUploadApi.Models
{
    public class BlobFileModel
    {
        public string FileName { get; set; } = string.Empty;
        public DateTimeOffset? CreatedOn { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
