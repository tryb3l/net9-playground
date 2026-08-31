using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WebApp.Interfaces;
using WebApp.Utils;

namespace WebApp.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AttachmentService> _logger;
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    
    private const int MaxImageWidth = 8192;
    private const int MaxImageHeight = 8192;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, Size> ImageSizes = new()
    {
        { "large", new Size(1920, 1080) }, // Increased for better quality on modern screens
        { "medium", new Size(800, 450) },
        { "thumbnail", new Size(400, 225) }
    };

    public AttachmentService(IWebHostEnvironment env, ILogger<AttachmentService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(
        IFormFile file, string? subfolder)
    {
        if (file.Length > MaxFileSizeBytes)
        {
            return (null, $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB.");
        }
        
        subfolder = subfolder?.Trim('/', '\\') ?? string.Empty;
        
        if (subfolder.Contains("..") || Path.IsPathRooted(subfolder))
        {
            _logger.LogWarning("Potential path traversal attempt: {Subfolder}", subfolder);
            return (null, "Invalid upload path.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (null, "Invalid file type. Only JPG, PNG, GIF, and WebP are allowed.");
        }

        var uploadsFolderPath = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        var createdFilePaths = new List<string>();

        try
        {
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            await using var stream = file.OpenReadStream();
            
            var imageInfo = await Image.IdentifyAsync(stream);
            if (imageInfo.Width > MaxImageWidth || imageInfo.Height > MaxImageHeight)
            {
                return (null, $"Image dimensions ({imageInfo.Width}x{imageInfo.Height}) exceed maximum allowed size of {MaxImageWidth}x{MaxImageHeight} pixels.");
            }
            
            stream.Position = 0;
            
            using var image = await Image.LoadAsync(stream);
            
            var urls = new Dictionary<string, string>();
            var baseFileName = Guid.NewGuid().ToString();

            // Check if this is an editor upload (content image) or a featured image
            bool isEditorUpload = subfolder.Contains("editor", StringComparison.OrdinalIgnoreCase);

            if (isEditorUpload)
            {
                using var resizedImage = image.Clone(ctx =>
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(1920, 0), // 0 height means "maintain aspect ratio"
                        Mode = ResizeMode.Max
                    }));

                var fileName = $"{baseFileName}.webp";
                var filePath = Path.Combine(uploadsFolderPath, fileName);
                await resizedImage.SaveAsWebpAsync(filePath);
                createdFilePaths.Add(filePath);

                // Editor expects a single URL usually, we map it to "large" for consistency
                urls["large"] = $"/uploads/{subfolder}/{fileName}";
            }
            else
            {
                // For featured images: Generate standard sizes
                foreach (var (sizeName, targetSize) in ImageSizes)
                {
                    using var resizedImage = image.Clone(ctx =>
                        ctx.Resize(new ResizeOptions
                        {
                            Size = targetSize,
                            Mode = ResizeMode.Max
                        }));

                    var fileName = $"{baseFileName}-{sizeName}.webp";
                    var filePath = Path.Combine(uploadsFolderPath, fileName);

                    await resizedImage.SaveAsWebpAsync(filePath);
                    createdFilePaths.Add(filePath);
                    
                    urls[sizeName] = string.IsNullOrEmpty(subfolder) 
                        ? $"/uploads/{fileName}" 
                        : $"/uploads/{subfolder}/{fileName}";
                }
            }

            return (urls, null);
        }
        catch (UnknownImageFormatException)
        {
            CleanupFailedUploads(createdFilePaths);
            _logger.LogWarning("Failed to identify image format for file: {FileName}", file.FileName);
            return (null, "The file is not a valid image or is corrupted.");
        }
        catch (Exception ex)
        {
            CleanupFailedUploads(createdFilePaths);
            _logger.LogError(ex, "Error processing image {FileName}", file.FileName);
            return (null, "An error occurred while processing the image.");
        }
    }

    private void CleanupFailedUploads(List<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete partial upload: {FilePath}", path);
            }
        }
    }

    public void DeleteStoredImages(string? storedUrls)
    {
        foreach (var url in FeaturedImage.ParseUrls(storedUrls).Values.Distinct())
        {
            DeleteByUrl(url);
        }
    }

    public int DeleteUnreferencedImages(string subfolder, IReadOnlyCollection<string> referencedContent)
    {
        var folder = ResolveUploadsPath(subfolder);
        if (folder == null || !Directory.Exists(folder))
        {
            return 0;
        }

        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(folder))
        {
            var fileName = Path.GetFileName(path);
            if (referencedContent.Any(c => c.Contains(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            try
            {
                File.Delete(path);
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete unreferenced upload: {FilePath}", path);
            }
        }

        return deleted;
    }

    private void DeleteByUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            // Anything not served from our own uploads folder is not ours to delete.
            return;
        }

        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));

        if (!IsInsideUploads(fullPath))
        {
            _logger.LogWarning("Refusing to delete a path outside the uploads folder: {Url}", url);
            return;
        }

        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image file: {FilePath}", fullPath);
        }
    }

    private string? ResolveUploadsPath(string subfolder)
    {
        subfolder = subfolder.Trim('/', '\\');
        if (subfolder.Contains("..") || Path.IsPathRooted(subfolder))
        {
            _logger.LogWarning("Refusing to resolve suspicious uploads subfolder: {Subfolder}", subfolder);
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", subfolder));
        return IsInsideUploads(fullPath) ? fullPath : null;
    }

    private bool IsInsideUploads(string fullPath)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
        return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}