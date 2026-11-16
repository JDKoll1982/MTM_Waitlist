namespace MTM_Database_Deployment.Models;

/// <summary>
/// Outcome of an apply run, including anything that was skipped or reported.
/// </summary>
public sealed class Model_DeploymentResult
{
    public int AppliedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public int FailedCount { get; private set; }
    public int BenignSkipCount { get; private set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyList<string> Messages => _messages;
    public IReadOnlyList<string> Failures => _failureOrder;
    public IReadOnlyDictionary<string, int> BenignSkips => _benignSkips;
    public IReadOnlyDictionary<string, int> FailureCounts => _failures;

    private readonly List<string> _messages = new();
    private readonly List<string> _failureOrder = new();
    private readonly Dictionary<string, int> _benignSkips = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);

    public bool IsSuccess => _failures.Count == 0;

    public void MarkApplied(string message)
    {
        AppliedCount++;
        _messages.Add(message);
    }

    public void MarkSkipped(string message)
    {
        SkippedCount++;
        _messages.Add(message);
    }

    public void MarkBenignSkip(string message)
    {
        BenignSkipCount++;
        _benignSkips[message] = _benignSkips.TryGetValue(message, out var count) ? count + 1 : 1;
    }

    public void MarkFailed(string message)
    {
        FailedCount++;
        if (!_failures.TryGetValue(message, out var count))
        {
            _failures[message] = 1;
            _failureOrder.Add(message);
            return;
        }

        _failures[message] = count + 1;
    }

    public void AddMessage(string message) => _messages.Add(message);

    public string ToReportText()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"Applied: {AppliedCount} | Skipped: {SkippedCount} | Already applied: {BenignSkipCount} | Failed: {FailedCount}");
        builder.AppendLine($"Duration: {Duration:h\\:mm\\:ss}");
        builder.AppendLine(new string('-', 100));

        foreach (var message in _messages)
        {
            builder.AppendLine(message);
        }

        if (_benignSkips.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("ALREADY APPLIED (benign duplicates, nothing changed)");
            foreach (var entry in _benignSkips)
            {
                builder.AppendLine($"  - {entry.Key} (x{entry.Value})");
            }
        }

        if (_failureOrder.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("FAILURES");
            foreach (var failure in _failureOrder)
            {
                var count = _failures[failure];
                builder.AppendLine(count > 1 ? $"  - {failure} (x{count})" : $"  - {failure}");
            }
        }

        return builder.ToString();
    }
}
