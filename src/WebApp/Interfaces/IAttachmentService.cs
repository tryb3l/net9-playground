namespace WebApp.Interfaces;

public interface IAttachmentService
{
    Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder);
}