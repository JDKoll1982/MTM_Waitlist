using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The finished-product page: the item step's one finished-product card opens this to ask which kind is being picked
/// up, and the kind chosen is the item the request carries.
/// </summary>
/// <remarks>
/// <para>
/// The four kinds are the four catalogue pickups — finished goods, non-conforming stock, work in process and outside
/// service — drawn by their own names rather than by the label three of them share on the item step. Each keeps the
/// item it always was: the code, the extra steps it needs and its allotment are the catalogue's, so the request this
/// page raises is the request the old card raised (FR-007).
/// </para>
/// <para>
/// The page reports the item step, because it asks a question of that step. Back therefore returns to the item step
/// and Continue advances from it with the kind already recorded.
/// </para>
/// </remarks>
public sealed partial class GenericRequestProductPickupViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestProductPickupViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Item;

    public override string TitleKey => "GenericRequest_ProductPickup_Title.Text";

    public override string IntroKey => "GenericRequest_ProductPickup_Intro.Text";

    public override string EmptyKey => "GenericRequest_ProductPickup_Empty.Text";

    /// <summary>The four kinds this request can be for, each one a card.</summary>
    public ObservableCollection<GenericRequestItemOption> Options { get; } = new();

    /// <summary>The kind the requester chose, or null before they choose one.</summary>
    [ObservableProperty]
    public partial GenericRequestItemOption? Selected { get; set; }

    /// <summary>One kind must be chosen, because the item step behind this page names none of them itself.</summary>
    protected override bool CanAnswer => Selected is not null;

    /// <summary>Back returns to the item step whose card opened this page.</summary>
    protected override GenericRequestStep BackStep => GenericRequestStep.Item;

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadItemsAsync(Draft.Category);

        Options.Clear();
        foreach (var option in result.Items)
        {
            if (!RequestItemCatalog.IsFinishedProductPickup(option.Code))
            {
                continue;
            }

            // The kind is drawn by its own name: three of the four share one label on the item step, which is the
            // label of the card this page was opened from and would name none of them here.
            var name = RequestItemCatalog.ResolveCatalogItemName(option.Code);

            Options.Add(string.IsNullOrWhiteSpace(name) ? option : option with { Name = name });
        }

        Selected = Options.FirstOrDefault(
            option => string.Equals(option.Code, Draft.ItemCode, StringComparison.OrdinalIgnoreCase));

        Apply(result);

        // A category holding none of the four has nothing for this page to ask, which is an empty read rather than a
        // populated one that draws no card.
        if (Presentation == GenericRequestPresentation.Populated && Options.Count == 0)
        {
            Presentation = GenericRequestPresentation.Empty;
        }
    }

    protected override void CommitToDraft()
    {
        if (Selected is not { } selected)
        {
            return;
        }

        Draft.ItemCode = selected.Code;
        Draft.ItemName = selected.Name;
        Draft.RequiresPart = selected.NeedsPart;
        Draft.RequiresDunnage = selected.NeedsDunnage;
        Draft.RequiresScrap = selected.NeedsScrap;
        Draft.RequiresDie = selected.NeedsDie;

        // None of the four asks a question of its own, and a kind changed here cannot keep an answer given for the
        // kind it replaced.
        Draft.Answer = string.Empty;
        Draft.EstimatedMinutes = selected.AllottedMinutes;
    }
}
