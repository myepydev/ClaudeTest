using System.Threading.Channels;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// Bounded async queue for print jobs.
/// Jobs are enqueued from the UI and dequeued by the BackgroundService.
/// </summary>
public class PrintJobQueue
{
    private readonly Channel<int> _channel = Channel.CreateBounded<int>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    public async ValueTask EnqueueAsync(int jobId, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(jobId, ct);
    }

    public async ValueTask<int> DequeueAsync(CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}
