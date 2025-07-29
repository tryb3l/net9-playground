namespace WebApplication1.Models;

public class ServiceResult
{
    public bool Succeeded { get; private init; }
    public List<string> Errors { get; private init; } = [];

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    public static ServiceResult Failure(List<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}