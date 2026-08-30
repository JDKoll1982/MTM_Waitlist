namespace MTM_Waitlist.Module_Core.Models;

public sealed class ComputerRecord
{
    public long Id { get; init; }

    public string ComputerName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string MacAddressNormalized { get; init; } = string.Empty;

    public bool IsRegistered { get; init; }

    /// <summary>
    /// True when this registry row represents the computer the app is currently
    /// running on. Set at read/display time from the running machine's hostname;
    /// never persisted. The current computer's Delete action is hidden.
    /// </summary>
    public bool IsCurrentComputer { get; set; }

    /// <summary>
    /// Presentation-only display label required by the Computer First-Load design
    /// (Ref: 8): <c>{DisplayName} - {ComputerName}</c>. Falls back to the raw
    /// computer name when no display name is present. Never persisted.
    /// </summary>
    public string GetDisplayLabel()
    {
        var computerName = ComputerName.Trim();
        var displayName = DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return computerName;
        }

        return string.IsNullOrWhiteSpace(computerName)
            ? displayName
            : $"{displayName} - {computerName}";
    }
}
