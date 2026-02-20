using BarTenderWebPrintTester.Models;
using Microsoft.Extensions.Options;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// File Drop / Hotfolder integration: writes XML to local outbox,
/// then moves atomically to the BarTender-monitored hotfolder.
/// Supports both local paths and UNC paths (\\server\share\folder).
/// </summary>
public class HotfolderService
{
    private readonly ILogger<HotfolderService> _logger;
    private readonly BarTenderSettings _settings;

    public HotfolderService(ILogger<HotfolderService> logger, IOptions<BarTenderSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Write XML to hotfolder using atomic move pattern.
    /// Returns the final file path on success.
    /// </summary>
    public async Task<string> SendToHotfolderAsync(string xmlContent, string? targetPathOverride = null)
    {
        var hotfolderSettings = _settings.Hotfolder;
        var targetPath = targetPathOverride ?? hotfolderSettings.TargetPath;
        var outboxPath = hotfolderSettings.LocalOutboxPath;
        var extension = hotfolderSettings.FileExtension;
        var useAtomicMove = hotfolderSettings.UseAtomicMove;

        var uniqueName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}";

        // Ensure directories exist
        Directory.CreateDirectory(outboxPath);
        Directory.CreateDirectory(targetPath);

        if (useAtomicMove)
        {
            // Write to outbox with .partial extension, then move atomically
            var partialPath = Path.Combine(outboxPath, uniqueName + ".partial");
            var finalPath = Path.Combine(targetPath, uniqueName);

            _logger.LogInformation("Writing XML to outbox: {PartialPath}", partialPath);
            await File.WriteAllTextAsync(partialPath, xmlContent, System.Text.Encoding.UTF8);

            _logger.LogInformation("Moving to hotfolder: {FinalPath}", finalPath);
            File.Move(partialPath, finalPath);

            _logger.LogInformation("File delivered to hotfolder: {FinalPath}", finalPath);
            return finalPath;
        }
        else
        {
            // Direct write to hotfolder (less safe, BarTender may read partial)
            var finalPath = Path.Combine(targetPath, uniqueName);
            _logger.LogInformation("Writing XML directly to hotfolder: {FinalPath}", finalPath);
            await File.WriteAllTextAsync(finalPath, xmlContent, System.Text.Encoding.UTF8);
            return finalPath;
        }
    }

    /// <summary>
    /// Send with retry on transient errors (network, lock, share unavailable).
    /// </summary>
    public async Task<string> SendWithRetryAsync(string xmlContent, string? targetPathOverride = null)
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
                    _logger.LogWarning("Retry attempt {Attempt}/{Max} after {Delay}s delay",
                        attempt, retryPolicy.MaxRetries, delay);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }

                return await SendToHotfolderAsync(xmlContent, targetPathOverride);
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Transient error on attempt {Attempt}", attempt + 1);
            }
        }

        throw new IOException(
            $"Failed to deliver file after {retryPolicy.MaxRetries + 1} attempts",
            lastException);
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or DirectoryNotFoundException;
    }
}
