using BarTenderWebPrintTester.Models;
using Microsoft.EntityFrameworkCore;

namespace BarTenderWebPrintTester.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrintJob>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.XmlContent).IsRequired();
        });
    }
}
