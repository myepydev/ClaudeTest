using System.ComponentModel.DataAnnotations;

namespace BarTenderWebPrintTester.Models;

public class PrintJob
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [Required]
    public string IntegrationMode { get; set; } = "FileDrop"; // FileDrop | Http

    public string TemplateName { get; set; } = "";
    public string TemplatePath { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public int Copies { get; set; } = 1;
    public bool IsDryRun { get; set; }

    [Required]
    public string XmlContent { get; set; } = "";

    public string Status { get; set; } = "Queued"; // Queued | Processing | Completed | Failed
    public string? ResultMessage { get; set; }
    public string? FilePath { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? HttpResponseBody { get; set; }
    public string? ErrorDetails { get; set; }
    public int RetryCount { get; set; }
}
