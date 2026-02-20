using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace BarTenderWebPrintTester.Logging;

/// <summary>
/// In-memory Serilog sink that keeps the last N log entries for UI display.
/// Thread-safe with a bounded buffer.
/// </summary>
public class InMemoryLogSink : ILogEventSink
{
    public static readonly InMemoryLogSink Instance = new();

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private const int MaxEntries = 500;

    public event Action? OnNewLogEntry;

    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.LocalDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
            SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc)
                ? sc.ToString().Trim('"')
                : null
        };

        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);

        OnNewLogEntry?.Invoke();
    }

    public IReadOnlyList<LogEntry> GetEntries() => _entries.ToArray();

    public IReadOnlyList<LogEntry> GetEntries(string? minLevel, int limit = 100)
    {
        var entries = _entries.ToArray().AsEnumerable();

        if (!string.IsNullOrEmpty(minLevel))
        {
            var levels = new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };
            var minIndex = Array.IndexOf(levels, minLevel);
            if (minIndex >= 0)
                entries = entries.Where(e => Array.IndexOf(levels, e.Level) >= minIndex);
        }

        return entries.TakeLast(limit).Reverse().ToArray();
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
    public string? SourceContext { get; set; }
}
