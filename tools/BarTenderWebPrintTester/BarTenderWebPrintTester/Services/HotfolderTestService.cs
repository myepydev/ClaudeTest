using System.Security.Principal;
using BarTenderWebPrintTester.Models;
using Microsoft.Extensions.Options;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// Tests access to a hotfolder path (local or UNC).
/// Verifies: existence, create file, rename/move, delete.
/// Reports process identity and detailed diagnostics.
/// </summary>
public class HotfolderTestService
{
    private readonly ILogger<HotfolderTestService> _logger;
    private readonly BarTenderSettings _settings;

    public HotfolderTestService(ILogger<HotfolderTestService> logger, IOptions<BarTenderSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<HotfolderTestResult> TestAccessAsync(string? pathOverride = null)
    {
        var result = new HotfolderTestResult();
        var path = pathOverride ?? _settings.Hotfolder.TargetPath;

        // Report process identity
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            result.ProcessIdentity = identity?.Name ?? Environment.UserName;
        }
        catch
        {
            result.ProcessIdentity = Environment.UserName;
        }

        result.TestedPath = path;
        _logger.LogInformation("Testing hotfolder access: {Path} as {Identity}",
            path, result.ProcessIdentity);

        // Step 1: Check directory exists
        result.Steps.Add(await TestStepAsync("Verifica esistenza cartella", () =>
        {
            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception ex) { throw new DirectoryNotFoundException($"Cartella non trovata e impossibile crearla: {ex.Message}"); }
                return Task.FromResult("Cartella creata con successo");
            }
            return Task.FromResult("Cartella esistente");
        }));

        if (!result.Steps.Last().Success)
        {
            result.OverallSuccess = false;
            return result;
        }

        var testFileName = $"__hotfolder_test_{Guid.NewGuid():N}.tmp";
        var testFilePath = Path.Combine(path, testFileName);
        var renamedPath = Path.Combine(path, testFileName.Replace(".tmp", ".xml"));

        // Step 2: Create temp file
        result.Steps.Add(await TestStepAsync("Creazione file temporaneo", async () =>
        {
            await File.WriteAllTextAsync(testFilePath, "<test>hotfolder access test</test>");
            return $"File creato: {testFileName}";
        }));

        // Step 3: Rename/move (atomic drop simulation)
        if (result.Steps.Last().Success)
        {
            result.Steps.Add(await TestStepAsync("Rename/Move (atomic drop)", () =>
            {
                File.Move(testFilePath, renamedPath);
                return Task.FromResult($"Rinominato a {Path.GetFileName(renamedPath)}");
            }));
        }

        // Step 4: Delete
        var fileToDelete = File.Exists(renamedPath) ? renamedPath : testFilePath;
        if (File.Exists(fileToDelete))
        {
            result.Steps.Add(await TestStepAsync("Eliminazione file di test", () =>
            {
                File.Delete(fileToDelete);
                return Task.FromResult("File eliminato");
            }));
        }

        result.OverallSuccess = result.Steps.All(s => s.Success);
        return result;
    }

    private async Task<TestStep> TestStepAsync(string name, Func<Task<string>> action)
    {
        var step = new TestStep { Name = name };
        try
        {
            step.Detail = await action();
            step.Success = true;
            _logger.LogInformation("Hotfolder test [{Step}]: OK - {Detail}", name, step.Detail);
        }
        catch (Exception ex)
        {
            step.Success = false;
            step.Detail = ex.Message;
            step.ErrorType = ex.GetType().Name;
            step.Suggestion = GetSuggestion(ex);
            _logger.LogWarning(ex, "Hotfolder test [{Step}]: FAILED", name);
        }
        return step;
    }

    private static string GetSuggestion(Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            "Permessi share/NTFS mancanti per l'identità IIS. Verificare che l'Application Pool identity abbia Read+Write+Modify sulla cartella UNC.",
        DirectoryNotFoundException =>
            "Share non raggiungibile. Verificare DNS, percorso UNC e permessi di rete.",
        IOException io when io.Message.Contains("network path") =>
            "Percorso di rete non trovato. Verificare rete/firewall/SMB e che il server sia raggiungibile.",
        IOException io when io.Message.Contains("logon") =>
            "Errore di autenticazione. Verificare credenziali account o che l'account non sia bloccato.",
        _ => "Errore imprevisto. Consultare i log per il dettaglio completo."
    };
}

public class HotfolderTestResult
{
    public string TestedPath { get; set; } = "";
    public string ProcessIdentity { get; set; } = "";
    public bool OverallSuccess { get; set; }
    public List<TestStep> Steps { get; set; } = new();
}

public class TestStep
{
    public string Name { get; set; } = "";
    public bool Success { get; set; }
    public string Detail { get; set; } = "";
    public string? ErrorType { get; set; }
    public string? Suggestion { get; set; }
}
