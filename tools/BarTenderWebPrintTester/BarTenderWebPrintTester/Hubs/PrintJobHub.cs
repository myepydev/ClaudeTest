using Microsoft.AspNetCore.SignalR;

namespace BarTenderWebPrintTester.Hubs;

/// <summary>
/// SignalR hub for real-time print job status updates.
/// </summary>
public class PrintJobHub : Hub
{
    public async Task JoinJobWatch(int jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
    }

    public async Task LeaveJobWatch(int jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
    }
}
