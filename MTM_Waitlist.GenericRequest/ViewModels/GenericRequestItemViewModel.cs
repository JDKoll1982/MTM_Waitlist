using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The item step: pick the item the request is about, and answer the one question that item's own configuration
/// declares.
/// </summary>
/// <remarks>
/// The question is asked here rather than on a later step because the work-order flow asks the same question in the
/// same place, so one item is answered identically whichever flow raised it (FR-006, FR-007, research R4). An item
/// whose configuration cannot be read never reaches this screen: the seam reports the step unavailable instead, so
/// no item is offered without the question it was supposed to ask (FR-021).
/// </remarks>
public sealed partial class GenericRequestItemViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestItemViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Item;

    public override string TitleKey => "GenericRequest_Item_Title.Text";

    public override string IntroKey => "GenericRequest_Item_Intro.Text";

    public override string EmptyKey => "GenericRequest_Item_Empty.Text";

    public ObservableCollection<GenericRequestItemOption> Options { get; } = new();

    /// <summary>The options the chosen item's enumerated question offers.</summary>
    public ObservableCollection<string> AnswerOptions { get; } = new();

    [ObservableProperty]
    public partial GenericRequestItemOption? Selected { get; set; }

    /// <summary>
    /// The requester's answer to the chosen item's question. One field for both kinds of question, because a text
    /// answer and a chosen option are the same thing to the draft and to the store.
    /// </summary>
    [ObservableProperty]
    public partial string Answer { get; set; } = string.Empty;

    /// <summary>The question the chosen item asks, or empty when it asks nothing.</summary>
    public string PromptText => Selected?.PromptText ?? string.Empty;

    /// <summary>Whether the chosen item asks anything at all.</summary>
    public bool HasQuestion => Selected?.AnswerKind is GenericRequestAnswerKind.Text or GenericRequestAnswerKind.Choice;

    /// <summary>Whether the question takes free text.</summary>
    public bool IsTextAnswer => Selected?.AnswerKind == GenericRequestAnswerKind.Text;

    /// <summary>Whether the question takes one of a fixed set of options.</summary>
    public bool IsChoiceAnswer => Selected?.AnswerKind == GenericRequestAnswerKind.Choice;

    /// <summary>Whether the answer given is incomplete, so the step can say why it will not move on.</summary>
    public bool IsAnswerIncomplete => HasQuestion && !IsAnswerComplete;

    /// <summary>Whether the question block is drawn at all.</summary>
    public Visibility QuestionVisibility => HasQuestion ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether the free-text box is drawn.</summary>
    public Visibility TextAnswerVisibility => IsTextAnswer ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether the option list is drawn.</summary>
    public Visibility ChoiceAnswerVisibility => IsChoiceAnswer ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether the note saying the answer is incomplete is drawn.</summary>
    public Visibility IncompleteVisibility => IsAnswerIncomplete ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>The longest answer the question accepts, or 0 when it sets no ceiling.</summary>
    public int MaxAnswerLength => Selected?.MaxLength ?? 0;

    /// <summary>The note shown while the question is unanswered, worded for the person answering it.</summary>
    public string AnswerIncompleteText => "GenericRequest_Item_Answer_Incomplete.Text".GetLocalized();

    /// <summary>
    /// A question that does not apply is answered, so the step can be left. The finished-product card names no item
    /// of its own, so it can only leave the step once the page behind it has named the kind.
    /// </summary>
    protected override bool CanAnswer =>
        Selected is { IsFinishedProductGroup: true }
            ? RequestItemCatalog.IsFinishedProductPickup(Draft.ItemCode)
            : Selected is not null && IsAnswerComplete;

    private bool IsAnswerComplete
    {
        get
        {
            if (Selected is not { } selected)
            {
                return false;
            }

            return selected.AnswerKind switch
            {
                GenericRequestAnswerKind.None => true,
                GenericRequestAnswerKind.Choice => !string.IsNullOrWhiteSpace(Answer),
                _ => IsTextAnswerWithinLimits(selected),
            };
        }
    }

    /// <summary>
    /// Show the page that asks which kind of finished product this request is picking up.
    /// </summary>
    /// <remarks>
    /// The four finished-product pickups are one card here, because a person collecting the product is not choosing
    /// between finished goods, non-conforming stock, work in process and outside service by name — they know which
    /// one it is, and the page behind the card asks them. Clicking the card never selects it, so the item is only
    /// ever named by that page (FR-007).
    /// </remarks>
    [RelayCommand]
    private void OpenFinishedProductChooser() => Navigator.ShowFinishedProductChooser();

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadItemsAsync(Draft.Category);

        Options.Clear();
        var finishedProductCardAdded = false;
        foreach (var option in result.Items)
        {
            if (!RequestItemCatalog.IsFinishedProductPickup(option.Code))
            {
                Options.Add(option);
                continue;
            }

            // The first of the four takes the card and the rest are folded into it, so the card keeps the place in
            // the list the catalog gave that family.
            if (!finishedProductCardAdded)
            {
                finishedProductCardAdded = true;
                Options.Add(GenericRequestItemOption.FinishedProductGroup(
                    RequestItemCatalog.ResolveFinishedProductPickupLabel()));
            }
        }

        // The item is matched on its catalogue code, not its name: the finished-product pickups share one displayed
        // label, so a name would select whichever came first rather than the one that was chosen. A draft that
        // already names one of them is the card itself, which is how the step reads when it is returned to.
        Selected = Options.FirstOrDefault(option => string.Equals(option.Code, Draft.ItemCode, StringComparison.OrdinalIgnoreCase))
            ?? (RequestItemCatalog.IsFinishedProductPickup(Draft.ItemCode)
                ? Options.FirstOrDefault(option => option.IsFinishedProductGroup)
                : null);

        if (Selected is not null && string.Equals(Selected.Code, Draft.ItemCode, StringComparison.OrdinalIgnoreCase))
        {
            Answer = Draft.Answer;
        }

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is not { } selected)
        {
            return;
        }

        // The finished-product card names no item, so leaving the step with it chosen keeps the item the page behind
        // it recorded rather than writing a code no request can carry.
        if (selected.IsFinishedProductGroup)
        {
            return;
        }

        Draft.ItemCode = selected.Code;
        Draft.ItemName = selected.Name;
        Draft.RequiresPart = selected.NeedsPart;
        Draft.RequiresDunnage = selected.NeedsDunnage;
        Draft.RequiresScrap = selected.NeedsScrap;
        Draft.RequiresDie = selected.NeedsDie;
        Draft.Answer = Answer.Trim();
        Draft.EstimatedMinutes = selected.AllottedMinutes;
    }

    partial void OnSelectedChanged(GenericRequestItemOption? value)
    {
        AnswerOptions.Clear();
        foreach (var option in value?.AnswerOptions ?? Array.Empty<string>())
        {
            AnswerOptions.Add(option);
        }

        // A different item asks a different question, so an answer given for the previous item is not an answer to
        // this one and is cleared rather than carried across. The fill goes with it, because every answer it
        // supplied was a statement about the item the request no longer carries (FR-009). Only a *changed* item
        // counts: the first item a request ever picks has nothing before it to carry anything over from, and the
        // finished-product card names no item at all, so it changes nothing the step holds.
        if (value?.IsFinishedProductGroup != true
            && !string.Equals(value?.Code, Draft.ItemCode, StringComparison.OrdinalIgnoreCase))
        {
            Answer = string.Empty;

            if (Draft.ItemCode.Length > 0)
            {
                Draft.JobFill = null;
            }
        }

        OnPropertyChanged(nameof(PromptText));
        OnPropertyChanged(nameof(HasQuestion));
        OnPropertyChanged(nameof(IsTextAnswer));
        OnPropertyChanged(nameof(IsChoiceAnswer));
        OnPropertyChanged(nameof(IsAnswerIncomplete));
        OnPropertyChanged(nameof(QuestionVisibility));
        OnPropertyChanged(nameof(TextAnswerVisibility));
        OnPropertyChanged(nameof(ChoiceAnswerVisibility));
        OnPropertyChanged(nameof(IncompleteVisibility));
        OnPropertyChanged(nameof(MaxAnswerLength));
        NotifyAnswerChanged();
    }

    partial void OnAnswerChanged(string value)
    {
        OnPropertyChanged(nameof(IsAnswerIncomplete));
        OnPropertyChanged(nameof(IncompleteVisibility));
        NotifyAnswerChanged();
    }

    private bool IsTextAnswerWithinLimits(GenericRequestItemOption selected)
    {
        var trimmed = Answer.Trim();
        var minimum = Math.Max(0, selected.MinLength);

        return trimmed.Length >= minimum
            && (selected.MaxLength <= 0 || trimmed.Length <= selected.MaxLength);
    }
}
