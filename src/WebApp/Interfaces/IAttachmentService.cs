namespace WebApp.Interfaces;

public interface IAttachmentService
{
    Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder);
    
    void DeleteStoredImages(string? storedUrls);

    int DeleteUnreferencedImages(string subfolder, IReadOnlyCollection<string> referencedContent);
}