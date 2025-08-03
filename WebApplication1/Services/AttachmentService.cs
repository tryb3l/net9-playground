using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using WebApplication1.Interfaces;

namespace WebApplication1.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

    public AttachmentService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<(string? Url, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (null, "Invalid file type. Only JPG, PNG, GIF, and WebP are allowed.");
        }

        var uploadsFolderPath = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        if (!Directory.Exists(uploadsFolderPath))
        {
            Directory.CreateDirectory(uploadsFolderPath);
        }

        var tempFilePath = Path.GetTempFileName();
        try
        {
            await using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            using var image = await Image.LoadAsync(tempFilePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(1200, 0),
                Mode = ResizeMode.Max
            }));

            var webpEncoder = new WebpEncoder { Quality = 80 };
            var webpFileName = $"{Guid.NewGuid()}.webp";
            var finalFilePath = Path.Combine(uploadsFolderPath, webpFileName);
            
            await image.SaveAsync(finalFilePath, webpEncoder);
                
            var imageUrl = $"/uploads/{subfolder}/{webpFileName}";
            return (imageUrl, null);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or NotSupportedException)
        {
            return (null, "Invalid image format. The uploaded file is not a valid image.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}