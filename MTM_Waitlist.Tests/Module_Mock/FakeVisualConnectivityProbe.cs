using MTM_Waitlist.Mock.Contracts;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Test double for the connectivity probe: replays a scripted sequence of reachability results.
/// </summary>
public sealed class FakeVisualConnectivityProbe : IVisualConnectivityProbe
{
    private readonly Queue<bool> _results;

    /// <summary>Creates the probe with the results to replay, in order.</summary>
    /// <param name="results">
    /// Reachability results, consumed one per probe. Once exhausted, further probes report unreachable.
    /// </param>
    public FakeVisualConnectivityProbe(params bool[] results)
    {
        _results = new Queue<bool>(results);
    }

    /// <summary>How many times the probe was run.</summary>
    public int ProbeCount { get; private set; }

    /// <inheritdoc />
    public Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        ProbeCount++;
        return Task.FromResult(_results.Count > 0 && _results.Dequeue());
    }
}
