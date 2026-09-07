namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The mock-mode setting for one external data source, stored centrally (all_users scope) so it can be
/// shared across running clients.
/// </summary>
public sealed record MockSettingState
{
    public string SettingKey { get; init; } = string.Empty;

    public bool IsMockEnabled { get; init; }

    public bool IsPresent { get; init; }
}
