using NetVips;
using WebApp.Interfaces;
using WebApp.Utils;

namespace WebApp.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AttachmentService> _logger;
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    // Exact libvips loader names; Uhdr is the Ultra HDR JPEG flavour recent Android phones produce.
    private static readonly HashSet<string> AllowedLoaders =
    [
        "VipsForeignLoadJpegBuffer",
        "VipsForeignLoadUhdrBuffer",
        "VipsForeignLoadPngBuffer",
        "VipsForeignLoadNsgifBuffer",
        "VipsForeignLoadWebpBuffer"
    ];

    private const int MaxImageWidth = 8192;
    private const int MaxImageHeight = 8192;
    private const long MaxTotalPixels = (long)MaxImageWidth * MaxImageHeight;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int WebpQuality = 80;
    private const int UnboundedHeight = 10_000_000;
    // thumbnail ignores its own fail_on argument, so truncation must be requested through the loader options.
    private const string LoadOptions = "fail_on=truncated";
    private const string InvalidImageMessage = "The file is not a valid image or is corrupted.";
    private const string BusyMessage = "The server is busy processing another image. Please try again.";
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(30);

    // Image processing is CPU- and memory-heavy; one upload at a time keeps peak memory bounded.
    private static readonly SemaphoreSlim ProcessingGate = new(1, 1);

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
            byte[] bytes;
            await using (var stream = file.OpenReadStream())
            using (var buffer = new MemoryStream())
            {
                await stream.CopyToAsync(buffer);
                bytes = buffer.ToArray();
            }

            // Check if this is an editor upload (content image) or a featured image
            var isEditorUpload = subfolder.Contains("editor", StringComparison.OrdinalIgnoreCase);

            if (!await ProcessingGate.WaitAsync(GateTimeout))
            {
                _logger.LogWarning("Image processing gate timed out for file: {FileName}", file.FileName);
                return (null, BusyMessage);
            }

            try
            {
                return await Task.Run(() =>
                    EncodeVariants(bytes, file.FileName, isEditorUpload, subfolder, uploadsFolderPath, createdFilePaths));
            }
            finally
            {
                ProcessingGate.Release();
            }
        }
        catch (VipsException ex)
        {
            CleanupFailedUploads(createdFilePaths);
            _logger.LogWarning(ex, "Failed to decode image: {FileName}", file.FileName);
            return (null, InvalidImageMessage);
        }
        catch (Exception ex)
        {
            CleanupFailedUploads(createdFilePaths);
            _logger.LogError(ex, "Error processing image {FileName}", file.FileName);
            return (null, "An error occurred while processing the image.");
        }
    }

    private (Dictionary<string, string>? Urls, string? ErrorMessage) EncodeVariants(
        byte[] bytes, string fileName, bool isEditorUpload, string subfolder, string uploadsFolderPath,
        List<string> createdFilePaths)
    {
        var loader = Image.FindLoadBuffer(bytes);
        if (loader == null || !AllowedLoaders.Contains(loader))
        {
            _logger.LogWarning("Rejected unsupported or unrecognised image format for file: {FileName}", fileName);
            return (null, InvalidImageMessage);
        }

        int width, height;
        long pages;
        using (var header = Image.NewFromBuffer(bytes))
        {
            width = header.Width;
            height = header.Height;
            pages = header.Contains("n-pages") ? Convert.ToInt64(header.Get("n-pages")) : 1;
        }

        if (width > MaxImageWidth || height > MaxImageHeight)
        {
            return (null, $"Image dimensions ({width}x{height}) exceed maximum allowed size of {MaxImageWidth}x{MaxImageHeight} pixels.");
        }

        if ((long)width * height * pages > MaxTotalPixels)
        {
            return (null, $"Image exceeds the maximum allowed total of {MaxTotalPixels} pixels across all frames.");
        }

        Directory.CreateDirectory(uploadsFolderPath);

        var urls = new Dictionary<string, string>();
        var baseFileName = Guid.NewGuid().ToString();
        var urlPrefix = string.IsNullOrEmpty(subfolder) ? "/uploads" : $"/uploads/{subfolder}";

        // Editor uploads may be animated; featured images are always a single frame.
        var keepAnimation = isEditorUpload && pages > 1;
        var variants = isEditorUpload ? [(ImageVariants.Large, ImageVariants.EditorWidth)] : ImageVariants.Featured;

        foreach (var (name, targetWidth) in variants)
        {
            using var resized = Image.ThumbnailBuffer(
                bytes,
                targetWidth,
                optionString: keepAnimation ? $"{LoadOptions},n=-1" : LoadOptions,
                height: UnboundedHeight,
                size: Enums.Size.Down);
            var encoded = resized.WebpsaveBuffer(q: WebpQuality, keep: Enums.ForeignKeep.None);

            var variantFileName = isEditorUpload ? $"{baseFileName}.webp" : $"{baseFileName}-{name}.webp";
            var filePath = Path.Combine(uploadsFolderPath, variantFileName);
            File.WriteAllBytes(filePath, encoded);
            createdFilePaths.Add(filePath);

            urls[name] = $"{urlPrefix}/{variantFileName}";
        }

        return (urls, null);
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