using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable tile on the New Request Item step, built <b>from configuration</b>: the Item, the name a
/// person reads, the Category it hangs under, whether it captures an answer, and whether its stored
/// configuration row was found at all.
/// </summary>
/// <remarks>
/// <see cref="IsConfigured"/> exists so the step can report a catalogued Item with no configuration row as
/// unavailable in plain language (FR-014) instead of offering it and failing later, or proceeding
/// half-configured. The tile carries no name of its own: the display name is resolved from the Item's resource
/// key so the tile never invents a label (FR-022).
/// </remarks>
public sealed class NewRequestItemOption
{
    /// <summary>The catalogued Item this tile offers.</summary>
    public RequestItemDefinition? Item { get; init; }

    /// <summary>The name a person reads, resolved through the resource mechanism.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>The Category the Item hangs under.</summary>
    public RequestCategory Category { get; init; }

    /// <summary>Whether the Item asks for one answer before confirmation.</summary>
    public bool CapturesAnswer { get; init; }

    /// <summary>
    /// False when the Item has no usable configuration row, or when that row could not be read. The step then
    /// stops the flow with the plain-language report rather than proceeding (FR-014, FR-026).
    /// </summary>
    public bool IsConfigured { get; init; } = true;

    /// <summary>Secondary line under the name — says what the Item will ask for, or that it asks nothing.</summary>
    public string Summary { get; init; } = string.Empty;
}
