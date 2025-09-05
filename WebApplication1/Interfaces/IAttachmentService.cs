namespace WebApplication1.Interfaces;

public interface IAttachmentService
{
    Task<(Dictionary<string, string>? Urls, string? ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder);
}