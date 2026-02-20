namespace BarTenderWebPrintTester.Models;

public class BarTenderSettings
{
    public string DefaultIntegrationMode { get; set; } = "FileDrop";
    public HotfolderSettings Hotfolder { get; set; } = new();
    public HttpSettings Http { get; set; } = new();
    public RetryPolicySettings RetryPolicy { get; set; } = new();
    public List<TemplateEntry> Templates { get; set; } = new();
    public List<string> Printers { get; set; } = new();
}

public class HotfolderSettings
{
    public string LocalOutboxPath { get; set; } = "./outbox";
    public string TargetPath { get; set; } = "";
    public string FileExtension { get; set; } = ".xml";
    public bool UseAtomicMove { get; set; } = true;
}

public class HttpSettings
{
    public string EndpointUrl { get; set; } = "";
    public string AuthMode { get; set; } = "None"; // None, Basic, Token
    public string AuthToken { get; set; } = "";
    public string AuthUsername { get; set; } = "";
    public string AuthPassword { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
}

public class RetryPolicySettings
{
    public int MaxRetries { get; set; } = 3;
    public int[] DelaySeconds { get; set; } = [1, 3, 5];
}

public class TemplateEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}
