using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// A single selectable card on the re-laid New Request Job Type step (Phase 3 Category→Item picker).
/// Unifies the three card shapes the grid can show so the page keeps one DataTemplate:
///   • canonical Category card  — <see cref="Category"/> set,
///   • canonical Item card      — <see cref="Item"/> set (submits its resolved legacy leaf),
///   • legacy Job-Type card      — <see cref="RequestType"/> set (DB-down fallback path).
/// Exactly one of Category / Item / RequestType is populated for a given tile.
/// </summary>
public sealed class NewRequestPickerTile
{
    /// <summary>Display name shown on the card (category name, canonical item name, or legacy type name).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Secondary line under the name (item count, canonical summary, or subtype count).</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Resolved image path for the card; empty for category tiles.</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Set when this tile is a canonical umbrella Category (drills to its Items).</summary>
    public RequestCategory? Category { get; init; }

    /// <summary>Set when this tile is a canonical Item (submits its resolved legacy leaf).</summary>
    public NewRequestCanonicalItem? Item { get; init; }

    /// <summary>Set when this tile is a legacy Job-Type card on the DB-down fallback path.</summary>
    public NewRequestTypeDefinition? RequestType { get; init; }

    public bool IsCategoryTile => Category is not null;

    public bool IsItemTile => Item is not null;

    public bool IsLegacyTypeTile => RequestType is not null;
}
