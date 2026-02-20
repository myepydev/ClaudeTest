using BarTenderWebPrintTester.Data;
using BarTenderWebPrintTester.Models;
using Microsoft.EntityFrameworkCore;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// CRUD and query operations for print jobs.
/// </summary>
public class PrintJobService
{
    private readonly AppDbContext _db;

    public PrintJobService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PrintJob> CreateJobAsync(PrintJob job)
    {
        job.CreatedAt = DateTime.UtcNow;
        job.Status = "Queued";
        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<PrintJob?> GetJobAsync(int id)
    {
        return await _db.PrintJobs.FindAsync(id);
    }

    public async Task UpdateJobAsync(PrintJob job)
    {
        _db.PrintJobs.Update(job);
        await _db.SaveChangesAsync();
    }

    public async Task<List<PrintJob>> GetRecentJobsAsync(int count = 50)
    {
        return await _db.PrintJobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<PrintJob>> GetJobsFilteredAsync(
        DateTime? from, DateTime? to,
        string? status, string? mode,
        int limit = 100)
    {
        var query = _db.PrintJobs.AsQueryable();

        if (from.HasValue)
            query = query.Where(j => j.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(j => j.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(j => j.Status == status);
        if (!string.IsNullOrEmpty(mode))
            query = query.Where(j => j.IntegrationMode == mode);

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(int Total, int Completed, int Failed, int Queued)> GetStats24hAsync()
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var jobs = await _db.PrintJobs
            .Where(j => j.CreatedAt >= since)
            .ToListAsync();

        return (
            jobs.Count,
            jobs.Count(j => j.Status == "Completed"),
            jobs.Count(j => j.Status == "Failed"),
            jobs.Count(j => j.Status is "Queued" or "Processing")
        );
    }

    public async Task DeleteJobAsync(int id)
    {
        var job = await _db.PrintJobs.FindAsync(id);
        if (job != null)
        {
            _db.PrintJobs.Remove(job);
            await _db.SaveChangesAsync();
        }
    }
}
