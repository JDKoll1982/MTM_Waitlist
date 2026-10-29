using System.Globalization;

using MTM_Waitlist.GenericRequest.Controls.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Controls.Helpers;

/// <summary>
/// Which cells one variant of the generic card draws.
/// </summary>
/// <remarks>
/// The card draws its own facts and nothing belonging to a work order, and it never draws a cell it has no value
/// for: an absent value produces no cell rather than a blank or a placeholder. Keeping both rules here means the
/// card and its preview cannot disagree about either.
/// </remarks>
public static class GenericRequestCardRules
{
    /// <summary>The cells <paramref name="model"/>'s variant draws, in reading order.</summary>
    public static IReadOnlyList<GenericRequestCardCell> CellsFor(GenericRequestCardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var cells = new List<GenericRequestCardCell>();

        Add(cells, "GenericRequest_Card_WorkCenter.Label", model.WorkCenter);
        Add(cells, "GenericRequest_Card_Item.Label", model.ItemName);

        switch (model.Variant)
        {
            case GenericRequestCardVariant.Part:
                Add(cells, "GenericRequest_Card_Part.Label", model.PartNumber);
                break;

            case GenericRequestCardVariant.Dunnage:
                Add(cells, "GenericRequest_Card_Dunnage.Label", model.DunnageType);
                break;

            case GenericRequestCardVariant.Scrap:
                Add(cells, "GenericRequest_Card_Scrap.Label", model.ScrapType);
                break;

            case GenericRequestCardVariant.Die:
                Add(cells, "GenericRequest_Card_Die.Label", model.DieNumber);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(model), model.Variant, "The card has no cells for that variant.");
        }

        if (model.Quantity is > 0)
        {
            Add(cells, "GenericRequest_Card_Quantity.Label", model.Quantity.Value.ToString("0.##", CultureInfo.CurrentCulture));
        }

        if (model.NeededBy is { } neededBy)
        {
            Add(cells, "GenericRequest_Card_NeededBy.Label", neededBy.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
        }

        return cells;
    }

    /// <summary>The variant's own name, for the card's header.</summary>
    public static string VariantLabel(GenericRequestCardVariant variant) => variant switch
    {
        GenericRequestCardVariant.Part => "GenericRequest_Card_Kind_Part.Text".GetLocalized(),
        GenericRequestCardVariant.Dunnage => "GenericRequest_Card_Kind_Dunnage.Text".GetLocalized(),
        GenericRequestCardVariant.Scrap => "GenericRequest_Card_Kind_Scrap.Text".GetLocalized(),
        GenericRequestCardVariant.Die => "GenericRequest_Card_Kind_Die.Text".GetLocalized(),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "The card has no name for that variant."),
    };

    private static void Add(List<GenericRequestCardCell> cells, string labelKey, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        cells.Add(new GenericRequestCardCell(labelKey.GetLocalized(), value));
    }
}
