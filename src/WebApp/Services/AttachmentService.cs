using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using WebApp.Interfaces;

namespace WebApp.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    private static readonly Dictionary<string, Size> ImageSizes = new()
    {
        { "large", new Size(1280, 720) },
        { "medium", new Size(800, 450) },
        { "thumbnail", new Size(400, 225) }
    };

    public AttachmentService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder)
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
            var urls = new Dictionary<string, string>();
            var baseFileName = Guid.NewGuid().ToString();

            var originalWebpEncoder = new WebpEncoder { Quality = 90 };
            var originalWebpFileName = $"{baseFileName}-original.webp";
            var originalFinalFilePath = Path.Combine(uploadsFolderPath, originalWebpFileName);
            await image.SaveAsync(originalFinalFilePath, originalWebpEncoder);
            urls["original"] = $"/uploads/{subfolder}/{originalWebpFileName}";


            foreach (var (key, size) in ImageSizes)
            {
                var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = size,
                    Mode = ResizeMode.Pad,
                    PadColor = Color.Transparent
                }));

                var webpEncoder = new WebpEncoder { Quality = 90 };
                var webpFileName = $"{baseFileName}-{key}.webp";
                var finalFilePath = Path.Combine(uploadsFolderPath, webpFileName);
                await resizedImage.SaveAsync(finalFilePath, webpEncoder);
                urls[key] = $"/uploads/{subfolder}/{webpFileName}";
            }

            return (urls, null);
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