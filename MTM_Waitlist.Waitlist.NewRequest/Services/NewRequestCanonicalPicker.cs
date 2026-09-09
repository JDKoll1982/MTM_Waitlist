using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure, side-effect-free assembly of the Phase 3 Category→Item picker option set for the New Request
/// wizard. The wizard's source of truth is still the real DB request-type tree
/// (<see cref="NewRequestTypeDefinition"/> / <see cref="NewRequestSubtypeDefinition"/>, whose leaf rows
/// carry the canonical <c>Category</c> + <c>ItemId</c> added in Phase 1.1). This helper:
///   • walks the eligibility-filtered tree leaves (subtype leaves + type leaves),
///   • groups them into canonical Category → Item groups (per <c>Request-Config-Template.csv</c>),
///   • resolves each group to ONE deterministic legacy leaf (the wizard must submit a legacy
///     RequestType + Subtype because downstream persistence is still legacy),
///   • orders categories Pickup → Deliver → Assist → Other and items by CSV Order.
/// Only groups with a submittable legacy leaf are surfaced (items with no DB leaf yet — e.g.
/// component / outside-service / dunnage — are excluded until their Phase 6.2/dunnage data source
/// lands). Deliberately DB-free and unit-testable.
/// </summary>
public static class NewRequestCanonicalPicker
{
    /// <summary>Canonical umbrella categories in display order.</summary>
    private static readonly RequestCategory[] CategoryOrder =
    [
        RequestCategory.Pickup,
        RequestCategory.Deliver,
        RequestCategory.Assist,
        RequestCategory.Other,
    ];

    /// <summary>Parses a DB <c>category</c> column value ("Pickup"/"Deliver"/"Assist"/"Other") case-insensitively.</summary>
    public static RequestCategory? TryParseCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? null : category!.Trim().ToLowerInvariant() switch
        {
            "pickup" => RequestCategory.Pickup,
            "deliver" => RequestCategory.Deliver,
            "assist" => RequestCategory.Assist,
            "other" => RequestCategory.Other,
            _ => null,
        };

    /// <summary>Display name for a canonical category.</summary>
    public static string CategoryDisplayName(RequestCategory category) => category switch
    {
        RequestCategory.Pickup => "Pickup",
        RequestCategory.Deliver => "Deliver",
        RequestCategory.Assist => "Assist",
        _ => "Other",
    };

    /// <summary>
    /// Builds the ordered Category → Item picker from the (already eligibility-filtered) DB request-type
    /// tree. Categories/items with no submittable legacy leaf are omitted.
    /// </summary>
    public static IReadOnlyList<NewRequestCanonicalCategory> BuildPicker(IReadOnlyList<NewRequestTypeDefinition> requestTypes)
    {
        var categoriesByKind = new Dictionary<RequestCategory, Dictionary<string, List<LeafCandidate>>>();

        foreach (var requestType in requestTypes ?? Array.Empty<NewRequestTypeDefinition>())
        {
            if (string.IsNullOrWhiteSpace(requestType.RequestType))
            {
                continue;
            }

            if (requestType.Subtypes.Count == 0)
            {
                AddCandidate(categoriesByKind, requestType, leafSubtype: null);
                continue;
            }

            foreach (var subtype in requestType.Subtypes)
            {
                AddCandidate(categoriesByKind, requestType, leafSubtype: subtype);
            }
        }

        var result = new List<NewRequestCanonicalCategory>();
        foreach (var category in CategoryOrder)
        {
            if (!categoriesByKind.TryGetValue(category, out var itemsByItemId) || itemsByItemId.Count == 0)
            {
                continue;
            }

            var items = new List<NewRequestCanonicalItem>();
            foreach (var (itemId, candidates) in itemsByItemId)
            {
                var canonical = RequestItemCatalog.FindById(itemId);
                var primary = SelectPrimaryLeaf(category, itemId, candidates);
                if (primary is null)
                {
                    continue;
                }

                var primaryName = primary.Subtype?.Name ?? primary.Type.RequestType;
                items.Add(new NewRequestCanonicalItem
                {
                    Category = category,
                    ItemId = itemId,
                    DisplayName = canonical?.DisplayName ?? primaryName,
                    UmbrellaVerb = canonical?.UmbrellaVerb ?? CategoryDisplayName(category),
                    Order = canonical?.Order ?? int.MaxValue,
                    RequestType = primary.Type.RequestType.Trim(),
                    Subtype = primary.Subtype?.Name?.Trim(),
                    RequiresTextInput = primary.Subtype?.RequiresTextInput == true
                        || (primary.Subtype is null && primary.Type.RequiresTextInput),
                    PromptText = primary.Subtype?.PromptText ?? primary.Type.PromptText,
                    MinLength = primary.Subtype?.MinLength ?? primary.Type.MinLength,
                    MaxLength = primary.Subtype?.MaxLength ?? primary.Type.MaxLength,
                    MergesLegacyLeaves = candidates.Count > 1,
                    Summary = candidates.Count == 1
                        ? primaryName
                        : $"Includes {candidates.Count} options",
                });
            }

            if (items.Count == 0)
            {
                continue;
            }

            result.Add(new NewRequestCanonicalCategory
            {
                Category = category,
                DisplayName = CategoryDisplayName(category),
                Items = items.OrderBy(item => item.Order).ToArray(),
            });
        }

        return result;
    }

    private static void AddCandidate(
        Dictionary<RequestCategory, Dictionary<string, List<LeafCandidate>>> categoriesByKind,
        NewRequestTypeDefinition requestType,
        NewRequestSubtypeDefinition? leafSubtype)
    {
        var category = leafSubtype is null
            ? TryParseCategory(requestType.Category)
            : TryParseCategory(leafSubtype.Category);
        if (category is null)
        {
            return;
        }

        var itemId = leafSubtype is null
            ? NormalizeItemId(requestType.ItemId)
            : NormalizeItemId(leafSubtype.ItemId);
        if (itemId is null)
        {
            return;
        }

        if (!categoriesByKind.TryGetValue(category.Value, out var itemsByItemId))
        {
            itemsByItemId = new Dictionary<string, List<LeafCandidate>>(StringComparer.OrdinalIgnoreCase);
            categoriesByKind[category.Value] = itemsByItemId;
        }

        if (!itemsByItemId.TryGetValue(itemId, out var candidates))
        {
            candidates = new List<LeafCandidate>();
            itemsByItemId[itemId] = candidates;
        }

        candidates.Add(new LeafCandidate(requestType, leafSubtype));
    }

    private static string? NormalizeItemId(string? itemId)
        => string.IsNullOrWhiteSpace(itemId) ? null : itemId!.Trim().ToLowerInvariant();

    private static LeafCandidate? SelectPrimaryLeaf(RequestCategory category, string itemId, List<LeafCandidate> candidates)
    {
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Deterministic disambiguation for canonical items that merge multiple legacy leaves. The
        // preferred (RequestType, SubtypeName-or-null) pair is the leaf whose persisted shape the
        // canonical item is meant to represent (grounded in seed_waitlist_request_catalog).
        foreach (var preferred in PrimaryLeafPreferences(category, itemId))
        {
            var match = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.Type.RequestType, preferred.Type, StringComparison.OrdinalIgnoreCase)
                && (preferred.Subtype is null
                    ? candidate.Subtype is null
                    : string.Equals(candidate.Subtype?.Name, preferred.Subtype, StringComparison.OrdinalIgnoreCase)));
            if (match is not null)
            {
                return match;
            }
        }

        return candidates[0];
    }

    private static IEnumerable<(string Type, string? Subtype)> PrimaryLeafPreferences(RequestCategory category, string itemId)
        => (category, itemId) switch
        {
            (RequestCategory.Pickup, "pickup-coil") => [(Type: "Pickup", Subtype: "Pickup Coil")],
            (RequestCategory.Pickup, "pickup-die") => [(Type: "Die Handling", Subtype: "Pull Die and Put Away")],
            (RequestCategory.Other, "other") => [(Type: "Other", Subtype: "General Text Entry")],
            _ => Array.Empty<(string Type, string? Subtype)>(),
        };

    private sealed class LeafCandidate
    {
        public LeafCandidate(NewRequestTypeDefinition type, NewRequestSubtypeDefinition? subtype)
        {
            Type = type;
            Subtype = subtype;
        }

        public NewRequestTypeDefinition Type { get; }

        public NewRequestSubtypeDefinition? Subtype { get; }
    }
}
