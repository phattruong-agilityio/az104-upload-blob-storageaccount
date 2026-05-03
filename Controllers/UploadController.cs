using AzureBlobUploadApi.Services;
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
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                var url = await _blobService.UploadFileAsync(file);
                return Ok(new { Message = "Upload successful", Url = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
