using System.Globalization;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Controls.Models;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The card comparison: the generic card in one of its four variants beside a work-order card, so the two can be
/// judged in one list.
/// </summary>
public sealed partial class TempPreviewCardPage : UserControl
{
    public TempPreviewCardPage(GenericRequestCardVariant variant)
    {
        Variant = variant;
        InitializeComponent();
    }

    /// <summary>Which arrangement of the generic card this comparison shows.</summary>
    public GenericRequestCardVariant Variant { get; }

    public string WorkOrderNumber => "0845-2213";

    public string WorkOrderPart => "12GA-CS-0001";

    public string WorkOrderSequence => "20";

    public string WorkOrderDue => DateTimeOffset.Now.Date.AddDays(1).AddHours(7).ToString("g", CultureInfo.CurrentCulture);

    public GenericRequestCardModel GenericModel => new()
    {
        RequestNumber = "GR-1042",
        Variant = Variant,
        WorkCenter = "Horizontal Saw Line West",
        ItemName = "Stated part",
        PartNumber = "5052/AL-1",
        DunnageType = "Aluminum sheet",
        ScrapType = "aluminum chips",
        DieNumber = "D-1001",
        Quantity = 12,
        NeededBy = DateTimeOffset.Now.Date.AddDays(1).AddHours(7),
        RaisedBy = "J. Koll",
        RaisedAt = DateTimeOffset.Now.AddHours(-2),
        History =
        [
            new GenericRequestCardHistoryEntry(DateTimeOffset.Now.AddHours(-2), "Raised from the waitlist"),
            new GenericRequestCardHistoryEntry(DateTimeOffset.Now.AddHours(-1), "Picked up by the handler"),
        ],
    };
}
