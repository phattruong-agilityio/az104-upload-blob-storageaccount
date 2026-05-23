using System.Diagnostics;
using System.Text.Json;
using AzureBlobUploadApi.Services;
using AzureBlobUploadApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace AzureBlobUploadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController(IBlobService blobService) : ControllerBase
    {
        private readonly IBlobService _blobService = blobService;

        [HttpGet()]
        public async Task<IActionResult> GetFile()
        {
            try
            {
                var files = await _blobService.ListFilesAsync();
                
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost()]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded");

            if (string.IsNullOrWhiteSpace(request.SasUri))
                return BadRequest("sasUri is required. Call GET /api/upload/sas first.");

            if (!TryParseAndValidateSasUri(request.SasUri, out var parsedSasUri, out var validationError))
                return BadRequest(validationError);

            try
            {
                var url = await _blobService.UploadFileAsync(request.File, parsedSasUri);
                return Ok(new { Message = "Upload successful", Url = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("sas")]
        public async Task<IActionResult> GetContainerSasUri()
        {
            try
            {
                var sasUri = await _blobService.GenerateContainerSasUriAsync();
                var azureAccount = await TryGetAzureAccountInfoAsync();

                return Ok(new
                {
                    SasUri = sasUri.ToString(),
                    StorageAccountName = GetStorageAccountNameFromBlobUri(sasUri),
                    AzureAccount = azureAccount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private static string GetStorageAccountNameFromBlobUri(Uri blobUri)
        {
            var hostParts = blobUri.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return hostParts.Length > 0 ? hostParts[0] : string.Empty;
        }

        private static async Task<object?> TryGetAzureAccountInfoAsync()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "az",
                        Arguments = "account show --output json",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                {
                    return new
                    {
                        Error = "Unable to read Azure account from CLI.",
                        Details = string.IsNullOrWhiteSpace(stderr) ? "az account show failed." : stderr.Trim()
                    };
                }

                using var doc = JsonDocument.Parse(stdout);
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = "Azure CLI not available or failed to execute.",
                    Details = ex.Message
                };
            }
        }

        private static bool TryParseAndValidateSasUri(string rawSasUri, out Uri sasUri, out string error)
        {
            sasUri = null!;
            error = string.Empty;

            var candidate = rawSasUri.Trim();

            // Typical client-side double-encoding patterns: %2B -> %252B, %3D -> %253D
            if (candidate.Contains("%252B", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("%253D", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("%252F", StringComparison.OrdinalIgnoreCase))
            {
                error = "sasUri looks double-encoded. Send raw SAS URI string from GET /api/upload/sas without encoding again.";
                return false;
            }

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsedUri) || parsedUri is null)
            {
                error = "sasUri is not a valid absolute URI.";
                return false;
            }

            sasUri = parsedUri;

            var query = sasUri.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "sasUri is missing query parameters.";
                return false;
            }

            if (!query.Contains("sig=", StringComparison.OrdinalIgnoreCase)
                || !query.Contains("sp=", StringComparison.OrdinalIgnoreCase)
                || !query.Contains("se=", StringComparison.OrdinalIgnoreCase)
                || !query.Contains("sr=", StringComparison.OrdinalIgnoreCase))
            {
                error = "sasUri is missing one or more required SAS parameters (sig, sp, se, sr).";
                return false;
            }

            // When + in sig is converted to space during form encoding, auth fails with AuthorizationPermissionMismatch.
            var signatureSegment = GetQuerySegment(candidate, "sig");
            if (!string.IsNullOrWhiteSpace(signatureSegment) && signatureSegment.Contains(' '))
            {
                error = "sasUri signature appears corrupted (contains spaces). Use multipart/form-data and do not re-encode the SAS URI.";
                return false;
            }

            return true;
        }

        private static string? GetQuerySegment(string rawUri, string key)
        {
            var queryStartIndex = rawUri.IndexOf('?', StringComparison.Ordinal);
            if (queryStartIndex < 0 || queryStartIndex == rawUri.Length - 1)
                return null;

            var query = rawUri[(queryStartIndex + 1)..];
            var segments = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (segment.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return segment;
            }

            return null;
        }
    }
}
