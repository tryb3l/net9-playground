using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WebApp.IntegrationTests;

internal sealed class LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var start = Stopwatch.StartNew();
        var response = await base.SendAsync(request, ct);
        start.Stop();

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogError(
            "HTTP {Method} {Uri} -> {StatusCode} in {ElapsedMilliseconds}ms\nResponse Body:\n{Body}",
            request.Method, request.RequestUri, (int)response.StatusCode, start.ElapsedMilliseconds, body);
        
        var attachmentName = $"response_{Guid.NewGuid():N}.html";
        TestContext.Current.AddAttachment(attachmentName, body);

        return response;
    }
}