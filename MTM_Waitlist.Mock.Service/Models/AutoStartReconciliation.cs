namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The result of reconciling the <c>autoStartAtLogon</c> setting with the real per-user <c>Run</c> entry.
/// </summary>
/// <remarks>
/// A mismatch is reported rather than silently ignored (FR-007): the setting is what the operator
/// configures, but the <i>effect</i> is the registry entry, and the two can diverge — a hand-edited
/// setting file, a removed registry value, or a moved executable all show up here.
/// </remarks>
public sealed record AutoStartReconciliation
{
    /// <summary>What the configuration says should be true.</summary>
    public required bool SettingEnabled { get; init; }

    /// <summary>Whether the per-user <c>Run</c> entry was present before reconciliation.</summary>
    public required bool WasRegisteredAtLogon { get; init; }

    /// <summary>Whether the registry entry was changed to match the setting.</summary>
    public required bool WasChanged { get; init; }

    /// <summary>Whether the registry now matches the setting.</summary>
    public required bool IsReconciled { get; init; }

    /// <summary>A secret-free explanation for the log and the status surface.</summary>
    public required string Message { get; init; }
}
