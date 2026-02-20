using BarTenderWebPrintTester.Data;
using BarTenderWebPrintTester.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// Background service that processes print jobs from the queue.
/// Handles both FileDrop and HTTP integration modes.
/// Sends real-time status updates via SignalR.
/// </summary>
public class PrintJobBackgroundService : BackgroundService
{
    private readonly ILogger<PrintJobBackgroundService> _logger;
    private readonly PrintJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PrintJobHub> _hubContext;

    public PrintJobBackgroundService(
        ILogger<PrintJobBackgroundService> logger,
        PrintJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<PrintJobHub> hubContext)
    {
        _logger = logger;
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Print job background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing print job");
            }
        }

        _logger.LogInformation("Print job background service stopped");
    }

    private async Task ProcessJobAsync(int jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<PrintJobService>();
        var hotfolderService = scope.ServiceProvider.GetRequiredService<HotfolderService>();
        var httpService = scope.ServiceProvider.GetRequiredService<HttpIntegrationService>();

        var job = await jobService.GetJobAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found in database", jobId);
            return;
        }

        _logger.LogInformation("Processing job {JobId} (mode: {Mode}, template: {Template})",
            jobId, job.IntegrationMode, job.TemplateName);

        job.Status = "Processing";
        await jobService.UpdateJobAsync(job);
        await NotifyJobStatusAsync(jobId, "Processing");

        try
        {
            if (job.IsDryRun)
            {
                _logger.LogInformation("Dry run for job {JobId} - validation only", jobId);
                job.Status = "Completed";
                job.ResultMessage = "Dry run completato con successo. XML validato, nessun invio effettuato.";
            }
            else if (job.IntegrationMode == "FileDrop")
            {
                var filePath = await hotfolderService.SendWithRetryAsync(job.XmlContent);
                job.Status = "Completed";
                job.FilePath = filePath;
                job.ResultMessage = $"File consegnato in hotfolder: {filePath}";
            }
            else if (job.IntegrationMode == "Http")
            {
                var result = await httpService.SendWithRetryAsync(job.XmlContent);
                job.HttpStatusCode = result.StatusCode;
                job.HttpResponseBody = result.ResponseBody;

                if (result.IsSuccess)
                {
                    job.Status = "Completed";
                    job.ResultMessage = $"HTTP {result.StatusCode} - Invio riuscito";
                }
                else
                {
                    job.Status = "Failed";
                    job.ResultMessage = $"HTTP {result.StatusCode} - Invio fallito";
                    job.ErrorDetails = result.ResponseBody;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", jobId);
            job.Status = "Failed";
            job.ResultMessage = $"Errore: {ex.Message}";
            job.ErrorDetails = ex.ToString();
        }

        job.CompletedAt = DateTime.UtcNow;
        await jobService.UpdateJobAsync(job);
        await NotifyJobStatusAsync(jobId, job.Status, job.ResultMessage);

        _logger.LogInformation("Job {JobId} completed with status: {Status}", jobId, job.Status);
    }

    private async Task NotifyJobStatusAsync(int jobId, string status, string? message = null)
    {
        await _hubContext.Clients.Group($"job-{jobId}")
            .SendAsync("JobStatusUpdate", new { jobId, status, message });
        await _hubContext.Clients.All
            .SendAsync("JobListUpdate", new { jobId, status });
    }
}
