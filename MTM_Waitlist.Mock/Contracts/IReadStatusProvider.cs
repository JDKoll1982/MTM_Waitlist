using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Exposes the current read state to the read-only status indicator.
/// </summary>
public interface IReadStatusProvider
{
    /// <summary>The latest snapshot.</summary>
    ReadStatusSnapshot Current { get; }

    /// <summary>Raised whenever the snapshot changes.</summary>
    event EventHandler<ReadStatusSnapshot>? Changed;
}
