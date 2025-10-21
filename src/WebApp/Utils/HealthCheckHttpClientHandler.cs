namespace WebApp.Utils;

public class HealthCheckHttpClientHandler : HttpClientHandler
{
    private readonly string _apiKey;

    public HealthCheckHttpClientHandler(string apiKey, bool isDevelopment)
    {
        _apiKey = apiKey;
        
        if (isDevelopment)
        {
            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(AdminOrApiKeyHandler.ApiKeyHeaderName, _apiKey);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}