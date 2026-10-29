using System.Globalization;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>A work center the request can belong to.</summary>
public sealed record GenericRequestWorkCenterOption(string Name, string Building);

/// <summary>A request category offered by a work center.</summary>
public sealed record GenericRequestCategoryOption(string Name, string Description);

/// <summary>
/// An item a category offers. The three flags say which extra steps this item's request needs, so the flow can
/// skip a page that does not apply instead of showing it empty.
/// </summary>
public sealed record GenericRequestItemOption(
    string Code,
    string Name,
    bool NeedsDunnage,
    bool NeedsScrap,
    bool NeedsDie);

/// <summary>A part the item's request can name.</summary>
public sealed record GenericRequestPartOption(string PartNumber, string Description, string QuantityOnHand);

/// <summary>A sequence a part can be requested at.</summary>
public sealed record GenericRequestSequenceOption(string Sequence, string Description);

/// <summary>One dunnage type, with the part that stands in for it.</summary>
public sealed record GenericRequestDunnageOption(string TypeName, string PartNumber);

/// <summary>A scrap type, with how many luggers are free to use.</summary>
public sealed record GenericRequestScrapOption(string ScrapType, int AvailableLuggerCount)
{
    /// <summary>The lugger count, ready to draw.</summary>
    public string AvailableLuggerDisplay => AvailableLuggerCount.ToString(CultureInfo.CurrentCulture);
}

/// <summary>A die the search matched.</summary>
public sealed record GenericRequestDieOption(string DieNumber, string Description, string Location);
