namespace WebApp.Interfaces;

public interface IAttachmentService
{
    Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder);

    /// <summary>
    /// Deletes every file referenced by a stored featured-image value (all generated sizes).
    /// Safe to call with null; failures are logged, never thrown - losing a file is not a
    /// reason to fail the surrounding content operation.
    /// </summary>
    void DeleteStoredImages(string? storedUrls);

    /// <summary>
    /// Deletes files in <paramref name="subfolder"/> whose name appears in none of the
    /// referenced file names. Returns how many were removed.
    /// </summary>
    int DeleteUnreferencedImages(string subfolder, IReadOnlyCollection<string> referencedContent);
}