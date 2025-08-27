using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("[area]/api/[controller]")]
public class AttachmentsController : Controller
{
    private readonly IAttachmentService _attachmentService;
    private readonly ILogger<AttachmentsController> _logger;
    private static readonly string[] AllowedMimeTypes = ["image/png", "image/jpeg", "image/gif", "image/webp"];

    public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload([FromForm(Name = "filepond")] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > 10 * 1024 * 1024) // 10 MB limit
            return BadRequest("File size exceeds the 10MB limit.");

        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Invalid file MIME type." });
        }

        try
        {
            var (urls, errorMessage) = await _attachmentService.ProcessAndSaveImageAsync(file, "posts");

            if (errorMessage != null)
            {
                _logger.LogWarning("File upload failed for {FileName}: {Error}", file.FileName, errorMessage);
                return BadRequest(new { message = errorMessage });
            }

            _logger.LogInformation("Successfully uploaded file {FileName} and generated image sizes.", file.FileName);
            return Ok(new { urls });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during file upload for {FileName}", file.FileName);
            return StatusCode(500, new { message = "An unexpected server error occurred." });
        }
    }
}