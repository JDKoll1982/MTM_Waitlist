using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Pure legacy → canonical re-map for the Type/Category/Item refactor (2026-09-08).
/// Maps a legacy (RequestType, Subtype) pair — the values stored on waitlist requests and driven
/// by the DB catalog — onto a canonical <see cref="RequestItemDefinition"/> row so the whole app can
/// render/title/order by the uniform Category/Item taxonomy. Mirror of the canonical mapping seeded
/// into <c>waitlist_request_types</c>/<c>waitlist_request_subtypes</c> (leaf rows) and of the
/// TypeCategoryActionRefactor clarifications. Stable GUIDs (RequestTypeInventory/RequestSubtypeInventory)
/// are untouched; this only adds the canonical Category/Item interpretation on top.
/// </summary>
public static class RequestItemLegacyMapper
{
    /// <summary>
    /// Maps a legacy (RequestType, Subtype) pair onto its canonical item id, or null when the pair is
    /// not one of the known leaf rows (grouping types and unlisted subtypes fall through to null).
    /// Keys are "<UPPER TYPE>\u0001<UPPER SUBTYPE>"; subtype-less leaf types use "<UPPER TYPE>".
    /// </summary>
    public static RequestItemDefinition? Map(string? requestType, string? subtype)
    {
        var type = (requestType ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }

        var typeKey = type.ToUpperInvariant();
        var sub = (subtype ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(sub))
        {
            var key = typeKey + "\u0001" + sub.ToUpperInvariant();
            return LegacyToItemId.TryGetValue(key, out var itemId)
                ? RequestItemCatalog.FindById(itemId)
                : null;
        }

        // Type-level leaves (types that carry no subtypes) map by type name only.
        return TypeLeafToItemId.TryGetValue(typeKey, out var leafItemId)
            ? RequestItemCatalog.FindById(leafItemId)
            : null;
    }

    /// <summary>
    /// Canonical umbrella verb (Card Line 1) for a legacy pair, when it maps to a canonical row;
    /// otherwise the legacy request type verbatim (fallback preserves old display behavior).
    /// </summary>
    public static string ResolveUmbrellaVerb(string? requestType, string? subtype) =>
        Map(requestType, subtype)?.UmbrellaVerb
        ?? (string.IsNullOrWhiteSpace(requestType) ? string.Empty : requestType.Trim());

    /// <summary>
    /// All known legacy leaf keys (used by tests / diagnostics to prove the catalog is fully mapped).
    /// </summary>
    public static IReadOnlyCollection<string> KnownLeafKeys => LegacyToItemId.Keys.ToArray();

    // Legacy subtype leaf rows → canonical item id. Mirrors seed_waitlist_request_catalog.
    private static readonly IReadOnlyDictionary<string, string> LegacyToItemId =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Pickup
            ["PICKUP\u0001PICKUP OTHER"] = "other",
            ["PICKUP\u0001PICKUP NCM"] = "pickup-ncm",
            ["PICKUP\u0001PICKUP WIP"] = "pickup-wip",
            ["PICKUP\u0001PICKUP FG"] = "pickup-fg",
            ["PICKUP\u0001PICKUP COIL"] = "pickup-coil",
            ["PICKUP\u0001PICKUP FLATSTOCK"] = "pickup-coil",
            ["PICKUP\u0001OUTSIDE SERVICE"] = "pickup-outside-service",

            // Other
            ["OTHER\u0001GENERAL TEXT ENTRY"] = "other",

            // Coil
            ["COIL\u0001BRING"] = "deliver-coil",
            ["COIL\u0001PICKUP"] = "pickup-coil",
            ["COIL\u0001WRONG COIL @ PRESS"] = "deliver-wrong-coil",
            ["COIL\u0001NEED RISER TABLE"] = "deliver-riser-table",
            ["COIL\u0001NEED COIL TURNED AROUND"] = "assist-coil-turn",

            // Scrap
            ["SCRAP\u0001EMPTY"] = "pickup-scrap",
            ["SCRAP\u0001PICKUP HOPPER, DO NOT RETURN"] = "pickup-hopper",
            ["SCRAP\u0001BRING HOPPER"] = "deliver-hopper",

            // Flatstock
            ["FLATSTOCK\u0001BRING"] = "deliver-flatstock",
            ["FLATSTOCK\u0001PICKUP"] = "pickup-coil",
            ["FLATSTOCK\u0001WRONG FLATSTOCK @ WORKCENTER"] = "deliver-wrong-flatstock",

            // Table Handling
            ["TABLE HANDLING\u0001TABLE PLACE PARTS"] = "assist-table-place",
            ["TABLE HANDLING\u0001TABLE REMOVE PARTS"] = "assist-table-remove",

            // Die Handling
            ["DIE HANDLING\u0001BRING DIE"] = "deliver-die",
            ["DIE HANDLING\u0001PULL DIE AND PUT AWAY"] = "pickup-die",
            ["DIE HANDLING\u0001PULL DIE AND TAKE TO DIE SHOP"] = "pickup-die",
            ["DIE HANDLING\u0001PULL DIE AND LEAVE @ PRESS"] = "pickup-die",
        };

    // Type-level leaf rows (types with no subtypes) → canonical item id.
    private static readonly IReadOnlyDictionary<string, string> TypeLeafToItemId =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["FORKLIFT ASSIST"] = "other",
        };
}
