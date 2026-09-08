using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Friendly, professional card titles for waitlist requests, keyed by (request type, subtype).
/// The card badge already shows the request type (e.g. COIL, O/S), so these titles drop the redundant
/// "Type /" prefix and read as a clear action phrase.
///
/// EDIT THE TITLES HERE: each value below is the exact text shown as the request card title. Add or
/// change a row to reword any type/subtype. Unlisted combinations fall back to "Type / Subtype".
///
/// Type/Category/Item refactor (2026-09-08, Phase 1.2): <see cref="For"/> keeps producing the current
/// card phrase (visible UI unchanged until Phase 4), while <see cref="ResolveLine1"/> and
/// <see cref="ResolveItem"/> expose the canonical uniform-card model — Line 1 = umbrella verb
/// (Pickup/Deliver/Assist/Other) and the canonical <see cref="RequestItemDefinition"/> whose concrete
/// Line 2 identifier (coil number, part number, …) is supplied by the Phase 2/6 resolvers.
/// </summary>
public static class WaitlistRequestTitles
{
    private static readonly IReadOnlyDictionary<string, string> Titles = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Pickup
        ["PICKUP\u0001PICKUP NCM"] = "Pickup: NCM",
        ["PICKUP\u0001PICKUP WIP"] = "Pickup: WIP",
        ["PICKUP\u0001PICKUP FG"] = "Pickup: FG",
        ["PICKUP\u0001PICKUP COIL"] = "Return: Coil",
        ["PICKUP\u0001PICKUP FLATSTOCK"] = "Return: Flatstock",
        ["PICKUP\u0001PICKUP OTHER"] = "Pickup: Other",
        ["PICKUP\u0001OUTSIDE SERVICE"] = "Pickup: Outside Service",

        // Coil
        ["COIL\u0001BRING"] = "Deliver: Coil",
        ["COIL\u0001PICKUP"] = "Return: Coil",
        ["COIL\u0001WRONG COIL @ PRESS"] = "Deliver & Pickup: Wrong Coil at Work Center",
        ["COIL\u0001NEED RISER TABLE"] = "Deliver: Riser Table",
        ["COIL\u0001NEED COIL TURNED AROUND"] = "Assist: Coil Facing Wrong Way",

        // Scrap
        ["SCRAP\u0001EMPTY"] = "Scrap: Empty",
        ["SCRAP\u0001PICKUP HOPPER, DO NOT RETURN"] = "Pickup: Hopper (do not return)",
        ["SCRAP\u0001BRING HOPPER"] = "Deliver: Hopper",

        // Flatstock
        ["FLATSTOCK\u0001BRING"] = "Deliver: Flatstock",
        ["FLATSTOCK\u0001PICKUP"] = "Return: Flatstock",
        ["FLATSTOCK\u0001WRONG FLATSTOCK @ WORKCENTER"] = "Return: Wrong flatstock at work center",

        // Table Handling
        ["TABLE HANDLING\u0001TABLE PLACE PARTS"] = "Assist: Place parts on table",
        ["TABLE HANDLING\u0001TABLE REMOVE PARTS"] = "Assist: Remove parts from table",

        // Die Handling
        ["DIE HANDLING\u0001BRING DIE"] = "Deliver: Die",
        ["DIE HANDLING\u0001PULL DIE AND PUT AWAY"] = "Pull Die: Return to Location",
        ["DIE HANDLING\u0001PULL DIE AND TAKE TO DIE SHOP"] = "Pull Die: Take to Die Shop",
        ["DIE HANDLING\u0001PULL DIE AND LEAVE @ PRESS"] = "Pull Die: Leave at Press",

        // Other
        ["OTHER\u0001GENERAL TEXT ENTRY"] = "General Request",
    };

    /// <summary>
    /// Resolves the friendly card title for a request. Returns the request type alone when it has no
    /// subtype; otherwise the matched friendly phrase, or "Type / Subtype" as a fallback for unlisted rows.
    /// </summary>
    public static string For(string? requestType, string? subtype)
    {
        var type = (requestType ?? string.Empty).Trim();
        var sub = (subtype ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(sub))
        {
            return string.IsNullOrEmpty(type) ? (requestType ?? string.Empty) : type;
        }

        var key = type.ToUpperInvariant() + "\u0001" + sub.ToUpperInvariant();
        return Titles.TryGetValue(key, out var title) ? title : $"{type} / {sub}";
    }

    /// <summary>
    /// Card Line 1 = the umbrella verb for a legacy (RequestType, Subtype) pair. Returns the
    /// canonical category verb (Pickup/Deliver/Assist/Other) when the pair maps to a catalog row,
    /// otherwise the legacy request type verbatim (fallback preserves old display behavior).
    /// Additive Phase 1.2 accessor; the uniform 2-line card consumes it in Phase 4.
    /// </summary>
    public static string ResolveLine1(string? requestType, string? subtype) =>
        RequestItemLegacyMapper.ResolveUmbrellaVerb(requestType, subtype);

    /// <summary>
    /// The canonical <see cref="RequestItemDefinition"/> (CSV catalog row) for a legacy pair, or
    /// null when the pair is not one of the mapped leaf rows. The concrete Line 2 identifier
    /// (coil number, part number, die number, …) hangs off this item and is supplied by the
    /// Phase 2/6 resolvers; the service layer only exposes the item metadata here.
    /// </summary>
    public static RequestItemDefinition? ResolveItem(string? requestType, string? subtype) =>
        RequestItemLegacyMapper.Map(requestType, subtype);
}
