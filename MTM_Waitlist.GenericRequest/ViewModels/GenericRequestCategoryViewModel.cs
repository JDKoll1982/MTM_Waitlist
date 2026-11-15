using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The category step: pick the category the item list is narrowed to.
/// </summary>
/// <remarks>
/// <para>
/// The requester arrives here from the work center they chose, so the step is built the way that one is: the
/// categories are cards, a click marks the card it landed on, and the chosen card is still marked when the step is
/// returned to. A category carries its own one-line description, which is what the card's second line says.
/// </para>
/// <para>
/// Choosing a different category discards what was stated below it, for the reason changing the work center does: an
/// item, a part and every answer after them were statements about the category the request no longer belongs to.
/// </para>
/// </remarks>
public sealed partial class GenericRequestCategoryViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestCategoryViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Category;

    public override string TitleKey => "GenericRequest_Category_Title.Text";

    public override string IntroKey => "GenericRequest_Category_Intro.Text";

    public override string EmptyKey => "GenericRequest_Category_Empty.Text";

    /// <summary>The categories this work center offers, each one a card.</summary>
    public ObservableCollection<GenericRequestCategoryOption> Options { get; } = new();

    /// <summary>The category the requester has chosen, or <see langword="null"/> before they choose one.</summary>
    [ObservableProperty]
    public partial GenericRequestCategoryOption? Selected { get; set; }

    protected override bool CanAnswer => Selected is not null;

    /// <summary>
    /// Choose the card the requester clicked, marking it and clearing every other card.
    /// </summary>
    /// <param name="option">The card that was clicked.</param>
    /// <remarks>
    /// A card click chooses rather than leaving the step, so the forward action stays the frame's Continue and a
    /// person can change their mind without leaving. Every card is marked explicitly so the outline follows the click
    /// even when the clicked card was already the chosen one.
    /// </remarks>
    [RelayCommand]
    private void SelectCategory(GenericRequestCategoryOption? option)
    {
        if (option is null)
        {
            return;
        }

        foreach (var candidate in Options)
        {
            candidate.IsSelected = ReferenceEquals(candidate, option);
        }

        Selected = option;
        NotifyAnswerChanged();
    }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadCategoriesAsync(Draft.WorkCenter);

        Options.Clear();
        foreach (var option in result.Items)
        {
            option.IsSelected = string.Equals(option.Code, Draft.Category, StringComparison.OrdinalIgnoreCase);
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.IsSelected);

        Apply(result);
        NotifyAnswerChanged();
    }

    protected override void CommitToDraft()
    {
        if (Selected is not { } selected)
        {
            return;
        }

        if (string.Equals(Draft.Category, selected.Code, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Choosing a different category makes every answer below it a statement about a category the request no
        // longer belongs to, so they are cleared rather than carried into a request the person did not describe.
        Draft.Category = selected.Code;
        ClearAnswersBelowTheCategory();
    }

    /// <summary>
    /// Discard everything the flow asks after the category: the item, its answer and the extra steps that item
    /// required, with the part, sequence, quantity and die each of those steps recorded.
    /// </summary>
    /// <remarks>
    /// The work center, its building and the chosen category are kept, because they are what the requester has just
    /// stated. The description and any accepted warning go with the rest, because both were written about the item
    /// that is no longer chosen.
    /// </remarks>
    private void ClearAnswersBelowTheCategory()
    {
        Draft.ItemCode = string.Empty;
        Draft.ItemName = string.Empty;
        Draft.Answer = string.Empty;
        Draft.EstimatedMinutes = null;
        Draft.RequiresPart = false;
        Draft.RequiresDunnage = false;
        Draft.RequiresScrap = false;
        Draft.RequiresDie = false;

        Draft.PartNumber = string.Empty;
        Draft.Sequence = string.Empty;
        Draft.SequenceSource = string.Empty;
        Draft.PartStage = GenericRequestPartStage.Unknown;
        Draft.QuantityReading = null;
        Draft.Quantity = 1;

        Draft.DunnageType = string.Empty;
        Draft.DunnagePart = string.Empty;
        Draft.ScrapType = string.Empty;
        Draft.DieNumber = string.Empty;
        Draft.SelectedDies.Clear();

        Draft.Description = string.Empty;
        Draft.AcceptedWarnings.Clear();
    }
}
