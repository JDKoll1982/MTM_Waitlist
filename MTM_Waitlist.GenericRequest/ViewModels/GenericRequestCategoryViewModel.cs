using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The category step: pick the category the item list is narrowed to, and answer the offer to fill the request from
/// the job running at the work center.
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
/// <para>
/// The job-fill offer is asked here because this is the first step after the work center is settled on every entry
/// path: a launch from a work center tile opens this step directly, and a request started from the flow's own work
/// center step arrives here when that step is left (021 FR-001, research R1). It is a decision, never a page: the
/// flow's step order is unchanged.
/// </para>
/// </remarks>
public sealed partial class GenericRequestCategoryViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;
    private readonly IGenericRequestJobFillData _jobFillData;

    public GenericRequestCategoryViewModel(
        IGenericRequestStepData data,
        IGenericRequestJobFillData jobFillData,
        IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(jobFillData);

        _data = data;
        _jobFillData = jobFillData;

        AcceptFillCommand = new AsyncRelayCommand(AcceptFillAsync, () => OfferState == GenericRequestJobFillOfferState.Offered);
        DeclineFillCommand = new RelayCommand(DeclineFill, () => OfferState == GenericRequestJobFillOfferState.Offered);
        RetryFillCommand = new AsyncRelayCommand(AcceptFillAsync);
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

    /// <summary>Which of the four things the offer is doing, for this request.</summary>
    [ObservableProperty]
    public partial GenericRequestJobFillOfferState OfferState { get; set; }

    /// <summary>
    /// The running job's work order, which is what the offer names. Empty unless a job was named.
    /// </summary>
    [ObservableProperty]
    public partial string JobWorkOrder { get; set; } = string.Empty;

    /// <summary>When the accepted read was attempted, which only the unavailable panel shows.</summary>
    [ObservableProperty]
    public partial DateTimeOffset? FillAttemptedAtUtc { get; set; }

    /// <summary>Answer the offer by taking the job's values.</summary>
    public IAsyncRelayCommand AcceptFillCommand { get; }

    /// <summary>Answer the offer by leaving the request blank.</summary>
    public IRelayCommand DeclineFillCommand { get; }

    /// <summary>Read the job again after a failed read. Never asks the question a second time.</summary>
    public IAsyncRelayCommand RetryFillCommand { get; }

    /// <summary>Whether the offer is on screen and unanswered.</summary>
    public bool IsOfferVisible => OfferState == GenericRequestJobFillOfferState.Offered;

    /// <summary>Whether the step says which job the request was filled from.</summary>
    public bool IsFilledVisible =>
        OfferState == GenericRequestJobFillOfferState.Answered && Draft.JobFill?.HasAnySeededValue == true;

    /// <summary>Whether the accepted read failed, so the step reports it with the attempt time and a retry.</summary>
    public bool IsFillUnavailable => OfferState == GenericRequestJobFillOfferState.Unavailable;

    /// <summary>The offer's heading.</summary>
    public string OfferTitle => "GenericRequest_Category_JobFill_Title.Text".GetLocalized();

    /// <summary>The offer's question, naming the job and the work center it runs at.</summary>
    public string OfferQuestion => string.Format(
        CultureInfo.CurrentCulture,
        "GenericRequest_Category_JobFill_Question.Text".GetLocalized(),
        JobWorkOrder,
        Draft.WorkCenter);

    /// <summary>What the step says once the request was filled from the job.</summary>
    public string FilledLine => string.Format(
        CultureInfo.CurrentCulture,
        "GenericRequest_Category_JobFill_Filled.Text".GetLocalized(),
        Draft.JobFill?.WorkOrder ?? string.Empty);

    /// <summary>The step's own sentence for a job that could not be read.</summary>
    public string FillUnavailableText => "GenericRequest_Category_JobFill_Unavailable.Text".GetLocalized();

    /// <summary>The label the offer's two answers carry.</summary>
    public string AcceptContent => "GenericRequest_Category_JobFill_Accept.Content".GetLocalized();

    /// <summary>The label the offer's refusal carries.</summary>
    public string DeclineContent => "GenericRequest_Category_JobFill_Decline.Content".GetLocalized();

    /// <summary>The label the failed read's retry carries.</summary>
    public string RetryContent => "GenericRequest_StepFrame_Retry.Content".GetLocalized();

    /// <summary>The label the unavailable panel puts beside the time of the attempt.</summary>
    public string FillAttemptedLabel => "GenericRequest_Category_JobFill_Attempted.Text".GetLocalized();

    /// <summary>When the read was attempted, as a person reads it, or empty when it has not been attempted.</summary>
    public string FillAttemptedAtDisplay => FillAttemptedAtUtc is { } at
        ? at.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
        : string.Empty;

    public Visibility OfferVisibility => IsOfferVisible ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FilledVisibility => IsFilledVisible ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FillUnavailableVisibility => IsFillUnavailable ? Visibility.Visible : Visibility.Collapsed;

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
        await LoadOfferAsync();

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
        // longer belongs to, so they are cleared rather than carried into a request the person did not describe. A
        // *changed* category also drops the job fill, because the item the fill was taken for is gone with the rest
        // (FR-009); the first category a request picks has nothing before it to carry anything over from.
        var changedCategory = Draft.Category.Length > 0;

        Draft.Category = selected.Code;
        ClearAnswersBelowTheCategory();

        if (changedCategory)
        {
            Draft.JobFill = null;
        }
    }

    /// <summary>
    /// Ask whether a job is running, unless this request already answered the offer.
    /// </summary>
    /// <remarks>
    /// The probe names the job and reads none of its values, so nothing is taken from the job before the requester
    /// chooses (FR-002). A work center with no job, or a probe that failed, draws no offer: a question that cannot be
    /// named is not a question (FR-004).
    /// </remarks>
    private async Task LoadOfferAsync()
    {
        if (Draft.JobFillAnswered)
        {
            JobWorkOrder = Draft.JobFill?.WorkOrder ?? string.Empty;
            OfferState = GenericRequestJobFillOfferState.Answered;
            NotifyOfferChanged();
            return;
        }

        if (Draft.WorkCenter.Length == 0)
        {
            return;
        }

        var probe = await _jobFillData.ProbeAsync(Draft.WorkCenter);

        if (probe.Presentation != GenericRequestPresentation.Populated || probe.Items.Count == 0)
        {
            JobWorkOrder = string.Empty;
            OfferState = GenericRequestJobFillOfferState.NotChecked;
            NotifyOfferChanged();
            return;
        }

        JobWorkOrder = probe.Items[0].WorkOrder;
        OfferState = GenericRequestJobFillOfferState.Offered;
        NotifyOfferChanged();
    }

    /// <summary>
    /// Take the job's values, and say so when they could not be read.
    /// </summary>
    /// <remarks>
    /// The values are read only now, because the requester has accepted (FR-002). A failed read seeds nothing and
    /// reports the attempt with a retry, so a request is never presented as filled when it is not (FR-016). A job
    /// that went away between the offer and this read answers Empty, and the step reads as the one a work center with
    /// no job shows.
    /// </remarks>
    private async Task AcceptFillAsync()
    {
        var result = await _jobFillData.ReadAsync(Draft.WorkCenter);

        FillAttemptedAtUtc = result.AttemptedAtUtc;

        if (result.Presentation == GenericRequestPresentation.Populated && result.Items.Count > 0)
        {
            Draft.JobFill = result.Items[0];
            Draft.JobFillAnswered = true;
            OfferState = GenericRequestJobFillOfferState.Answered;
            NotifyOfferChanged();
            NotifyAnswerChanged();
            return;
        }

        if (result.Presentation == GenericRequestPresentation.Empty)
        {
            Draft.JobFill = null;
            Draft.JobFillAnswered = true;
            OfferState = GenericRequestJobFillOfferState.Answered;
            NotifyOfferChanged();
            return;
        }

        OfferState = GenericRequestJobFillOfferState.Unavailable;
        NotifyOfferChanged();
    }

    /// <summary>
    /// Answer the offer by leaving the request blank, for this request only.
    /// </summary>
    private void DeclineFill()
    {
        Draft.JobFill = null;
        Draft.JobFillAnswered = true;
        OfferState = GenericRequestJobFillOfferState.Answered;
        NotifyOfferChanged();
    }

    partial void OnOfferStateChanged(GenericRequestJobFillOfferState value) => NotifyOfferChanged();

    partial void OnJobWorkOrderChanged(string value) => NotifyOfferChanged();

    partial void OnFillAttemptedAtUtcChanged(DateTimeOffset? value) =>
        OnPropertyChanged(nameof(FillAttemptedAtDisplay));

    /// <summary>
    /// Tell the panel and the two answers what the offer now reads.
    /// </summary>
    private void NotifyOfferChanged()
    {
        OnPropertyChanged(nameof(IsOfferVisible));
        OnPropertyChanged(nameof(IsFilledVisible));
        OnPropertyChanged(nameof(IsFillUnavailable));
        OnPropertyChanged(nameof(OfferVisibility));
        OnPropertyChanged(nameof(FilledVisibility));
        OnPropertyChanged(nameof(FillUnavailableVisibility));
        OnPropertyChanged(nameof(OfferQuestion));
        OnPropertyChanged(nameof(FilledLine));
        OnPropertyChanged(nameof(FillAttemptedAtDisplay));
        AcceptFillCommand.NotifyCanExecuteChanged();
        DeclineFillCommand.NotifyCanExecuteChanged();
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
