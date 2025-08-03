namespace WebApplication1.Interfaces;

public interface IAttachmentService
{
    Task<(string? Url, string?ErrorMessage)> ProcessAndSaveImageAsync(IFormFile file, string subfolder);
}