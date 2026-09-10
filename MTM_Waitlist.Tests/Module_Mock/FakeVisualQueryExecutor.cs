using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Test double for the live Infor Visual executor: returns a configured classified outcome and records
/// what it was asked to run.
/// </summary>
public sealed class FakeVisualQueryExecutor : IVisualQueryExecutor
{
    private readonly Func<string, IReadOnlyDictionary<string, object?>, VisualQueryOutcome> _outcomeFactory;

    /// <summary>Creates an executor that always returns <paramref name="outcome"/>.</summary>
    public FakeVisualQueryExecutor(VisualQueryOutcome outcome)
        : this((_, _) => outcome)
    {
    }

    /// <summary>Creates an executor whose outcome depends on the script path and parameters.</summary>
    public FakeVisualQueryExecutor(
        Func<string, IReadOnlyDictionary<string, object?>, VisualQueryOutcome> outcomeFactory)
    {
        ArgumentNullException.ThrowIfNull(outcomeFactory);
        _outcomeFactory = outcomeFactory;
    }

    /// <summary>How many times a live read was attempted.</summary>
    public int ExecuteCount { get; private set; }

    /// <summary>The script path of the most recent attempt.</summary>
    public string? LastScriptPath { get; private set; }

    /// <summary>The parameters of the most recent attempt.</summary>
    public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

    /// <inheritdoc />
    public Task<VisualQueryOutcome> ExecuteAsync(
        string sourceScriptRelativePath,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ExecuteCount++;
        LastScriptPath = sourceScriptRelativePath;
        LastParameters = parameters;
        return Task.FromResult(_outcomeFactory(sourceScriptRelativePath, parameters));
    }
}
