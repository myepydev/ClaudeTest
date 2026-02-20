using BarTenderWebPrintTester.Data;
using BarTenderWebPrintTester.Hubs;
using BarTenderWebPrintTester.Logging;
using BarTenderWebPrintTester.Models;
using BarTenderWebPrintTester.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Sink(InMemoryLogSink.Instance)
            .Enrich.FromLogContext();
    });

    // Configuration
    builder.Services.Configure<BarTenderSettings>(
        builder.Configuration.GetSection("BarTender"));

    // EF Core + SQLite
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Services
    builder.Services.AddScoped<PrintJobService>();
    builder.Services.AddScoped<XmlGeneratorService>();
    builder.Services.AddScoped<HotfolderService>();
    builder.Services.AddScoped<HttpIntegrationService>();
    builder.Services.AddScoped<HotfolderTestService>();
    builder.Services.AddSingleton<PrintJobQueue>();
    builder.Services.AddHostedService<PrintJobBackgroundService>();
    builder.Services.AddSingleton(InMemoryLogSink.Instance);

    // Blazor + SignalR
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddSignalR();

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    // Ensure outbox directory
    var outboxPath = builder.Configuration["BarTender:Hotfolder:LocalOutboxPath"] ?? "./outbox";
    Directory.CreateDirectory(outboxPath);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<BarTenderWebPrintTester.Components.App>()
        .AddInteractiveServerRenderMode();

    app.MapHub<PrintJobHub>("/hubs/printjob");

    app.UseSerilogRequestLogging();

    Log.Information("BarTender Web Print Tester started");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
