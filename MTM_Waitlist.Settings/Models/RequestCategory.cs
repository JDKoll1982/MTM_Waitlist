namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Canonical umbrella Category for waitlist requests.
/// Type/Category refactor (2026-09-07): Category is the umbrella verb shown as the card Line 1
/// (Pickup / Deliver / Assist / Other); the Item (thing being handled) hangs underneath it.
/// The four Categories are fixed by specs/004-unified-card-item-picker/contracts/request-picker-flow.md
/// §4; the design documents that produced them are design-time inputs and are never shipped (FR-027).
/// </summary>
public enum RequestCategory
{
    /// <summary>Move a thing from another location to the requesting work center.</summary>
    Pickup,

    /// <summary>Deliver a thing to the requesting work center (destination is always the requester).</summary>
    Deliver,

    /// <summary>Assistance with a thing (turn, place, general help).</summary>
    Assist,

    /// <summary>Catch-all free-text request (formerly pickup-other + assist-forklift).</summary>
    Other
}
