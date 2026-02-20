using System.Net.Http.Headers;
using System.Text;
using BarTenderWebPrintTester.Models;
using Microsoft.Extensions.Options;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// HTTP POST integration: sends XML to a configurable endpoint
/// (e.g. BarTender Integration Service REST API).
/// </summary>
public class HttpIntegrationService
{
    private readonly ILogger<HttpIntegrationService> _logger;
    private readonly BarTenderSettings _settings;

    public HttpIntegrationService(ILogger<HttpIntegrationService> logger, IOptions<BarTenderSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<HttpIntegrationResult> SendAsync(string xmlContent, string? endpointOverride = null)
    {
        var httpSettings = _settings.Http;
        var url = endpointOverride ?? httpSettings.EndpointUrl;

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("HTTP endpoint URL is not configured");

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(httpSettings.TimeoutSeconds)
        };

        // Auth
        switch (httpSettings.AuthMode?.ToLowerInvariant())
        {
            case "basic":
                var basicAuth = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{httpSettings.AuthUsername}:{httpSettings.AuthPassword}"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", basicAuth);
                break;
            case "token":
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", httpSettings.AuthToken);
                break;
        }

        var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

        _logger.LogInformation("Sending XML via HTTP POST to {Url} ({Bytes} bytes)",
            url, Encoding.UTF8.GetByteCount(xmlContent));

        var response = await httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("HTTP response: {StatusCode}", (int)response.StatusCode);

        return new HttpIntegrationResult
        {
            StatusCode = (int)response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode,
            ResponseBody = responseBody
        };
    }

    public async Task<HttpIntegrationResult> SendWithRetryAsync(string xmlContent, string? endpointOverride = null)
    {
        var retryPolicy = _settings.RetryPolicy;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= retryPolicy.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var delayIndex = Math.Min(attempt - 1, retryPolicy.DelaySeconds.Length - 1);
                    var delay = retryPolicy.DelaySeconds[delayIndex];
                    _logger.LogWarning("HTTP retry attempt {Attempt}/{Max} after {Delay}s delay",
                        attempt, retryPolicy.MaxRetries, delay);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }

                var result = await SendAsync(xmlContent, endpointOverride);
                if (result.IsSuccess)
                    return result;

                // Non-success status code: treat 5xx as retryable
                if (result.StatusCode >= 500)
                {
                    lastException = new HttpRequestException(
                        $"Server error {result.StatusCode}: {result.ResponseBody}");
                    _logger.LogWarning("Server error {StatusCode} on attempt {Attempt}",
                        result.StatusCode, attempt + 1);
                    continue;
                }

                // 4xx: not retryable
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request error on attempt {Attempt}", attempt + 1);
            }
        }

        return new HttpIntegrationResult
        {
            StatusCode = 0,
            IsSuccess = false,
            ResponseBody = $"All {retryPolicy.MaxRetries + 1} attempts failed: {lastException?.Message}"
        };
    }
}

public class HttpIntegrationResult
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ResponseBody { get; set; } = "";
}
