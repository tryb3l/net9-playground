using System;

namespace WebApplication1.Models;

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public List<string> Errors { get; } = new List<string>();

    public static ServiceResult Success()
    {
        return new ServiceResult { Succeeded = true };
    }

    public static ServiceResult Failure(string error)
    {
        var result = new ServiceResult { Succeeded = false };
        result.Errors.Add(error);
        return result;
    }

    public static ServiceResult Failure(IEnumerable<string> errors)
    {
        var result = new ServiceResult { Succeeded = false };
        result.Errors.AddRange(errors);
        return result;
    }
}
