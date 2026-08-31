namespace WebApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; init; }

    /// <summary>Set when the page is reached via UseStatusCodePagesWithReExecute.</summary>
    public int? StatusCode { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Title => StatusCode switch
    {
        404 => "Page not found",
        403 => "Access denied",
        401 => "Sign in required",
        _ => "Something went wrong"
    };

    public string Message => StatusCode switch
    {
        404 => "That page doesn't exist, or the post has been unpublished or removed.",
        403 => "You don't have permission to view this page.",
        401 => "You need to sign in to view this page.",
        _ => "An error occurred while processing your request. It has been logged."
    };
}
