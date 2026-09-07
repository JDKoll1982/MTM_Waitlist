using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockFallbackDebouncer"/>
public sealed class MockFallbackDebouncer : IMockFallbackDebouncer
{
    private readonly Dictionary<ConnectionSource, SourceState> _state = new();
    private readonly object _lock = new();

    public MockFallbackDebouncer(int threshold = 2)
    {
        Threshold = threshold < 1 ? 1 : threshold;
    }

    public int Threshold { get; }

    public ConnectionHealthStatus Observe(ConnectionSource source, ConnectionHealthStatus rawStatus)
    {
        lock (_lock)
        {
            if (!_state.TryGetValue(source, out var state))
            {
                state = new SourceState { Stable = ConnectionHealthStatus.Unknown };
                _state[source] = state;
            }

            if (rawStatus == state.Stable)
            {
                // Confirmed same as current stable; clear any in-progress candidate count.
                state.Candidate = rawStatus;
                state.Count = 0;
                return state.Stable;
            }

            if (rawStatus == state.Candidate)
            {
                state.Count++;
            }
            else
            {
                state.Candidate = rawStatus;
                state.Count = 1;
            }

            if (state.Count >= Threshold)
            {
                state.Stable = rawStatus;
                state.Candidate = rawStatus;
                state.Count = 0;
            }

            return state.Stable;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state.Clear();
        }
    }

    private sealed class SourceState
    {
        public ConnectionHealthStatus Stable { get; set; }

        public ConnectionHealthStatus Candidate { get; set; }

        public int Count { get; set; }
    }
}
