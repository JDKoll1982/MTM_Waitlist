namespace MTM_Waitlist.Module_Shared.Models;

/// <summary>
/// A selectable computer shown in the UI. <see cref="Key"/> is the raw registry
/// <c>computer_name</c> (used as the stable lookup/selection key), while
/// <see cref="Label"/> is the presentation-only <c>{DisplayName} - {ComputerName}</c>
/// format required by the Computer First-Load design (Ref: 8). Stored data is never
/// rewritten; the label is derived at read/display time.
/// </summary>
public sealed class ComputerOption
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;
}
