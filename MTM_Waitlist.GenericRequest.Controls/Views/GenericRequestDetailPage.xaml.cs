using System.Globalization;

using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Controls.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Controls;

/// <summary>
/// One generic request in full: the card's facts, who raised it, when, and what has happened since.
/// </summary>
/// <remarks>
/// The requester, the time raised and the history live here and not on the card, because the card is what a handler
/// reads in a list and the detail page is what they open. The parameterless constructor exists because a page
/// activated through its XAML type cannot be handed a constructor argument.
/// </remarks>
public sealed partial class GenericRequestDetailPage : Page
{
    public GenericRequestDetailPage()
    {
        InitializeComponent();
    }

    public GenericRequestDetailPage(GenericRequestCardModel model)
        : this()
    {
        Show(model);
    }

    /// <summary>The request this page shows.</summary>
    public GenericRequestCardModel Model { get; private set; } = new();

    public string RaisedBy => string.IsNullOrWhiteSpace(Model.RaisedBy)
        ? "GenericRequest_Card_NoValue.Text".GetLocalized()
        : Model.RaisedBy;

    public string RaisedAt => Model.RaisedAt is { } raisedAt
        ? raisedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
        : "GenericRequest_Card_NoValue.Text".GetLocalized();

    public IReadOnlyList<GenericRequestCardHistoryEntry> History => Model.History;

    public string HistoryName => "GenericRequest_Card_History_Title.Text".GetLocalized();

    /// <summary>Show <paramref name="model"/>.</summary>
    public void Show(GenericRequestCardModel model)
    {
        Model = model;
        Bindings.Update();
    }
}
