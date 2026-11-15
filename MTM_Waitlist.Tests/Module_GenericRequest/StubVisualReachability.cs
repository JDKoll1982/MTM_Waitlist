using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The reachability probe's verdict, as a test states it.
/// </summary>
/// <remarks>
/// The probe itself is what establishes the verdict by asking Infor Visual twice; a read never decides it. A test
/// therefore only has to say what the probe has settled on, which is what makes "the read skipped the live attempt"
/// observable.
/// </remarks>
internal sealed class StubVisualReachability : IVisualReachabilityDetector
{
    public StubVisualReachability(VisualReadStatus status)
    {
        Current = status;
    }

    /// <summary>The verdict this stub reports, which a test may change between reads.</summary>
    public VisualReadStatus Current { get; set; }

    public event EventHandler<VisualReadStatus>? StateChanged;

    /// <summary>How many probes were run through this stub, which a read must never cause.</summary>
    public int ProbeCount { get; private set; }

    public Task ProbeAsync(CancellationToken cancellationToken = default)
    {
        ProbeCount++;
        StateChanged?.Invoke(this, Current);
        return Task.CompletedTask;
    }
}
