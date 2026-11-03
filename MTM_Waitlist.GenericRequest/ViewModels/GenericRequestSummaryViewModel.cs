using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>One label and the answer beside it, on the review step.</summary>
public sealed record GenericRequestSummaryRow(string Label, string Value);

/// <summary>
/// The summary step: every answer given, so the requester can check the request before raising it.
/// </summary>
public sealed partial class GenericRequestSummaryViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestSummaryViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Summary;

    public override string TitleKey => "GenericRequest_Summary_Title.Text";

    public override string IntroKey => "GenericRequest_Summary_Intro.Text";

    public override string EmptyKey => "GenericRequest_Summary_Empty.Text";

    public ObservableCollection<GenericRequestSummaryRow> Rows { get; } = new();

    /// <summary>Whether the request is being raised, so the forward button can be held while it is.</summary>
    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    /// <summary>
    /// Raise the request, then let the flow continue only when the queue accepted it.
    /// </summary>
    /// <remarks>
    /// A refusal keeps the requester on the review with the store's own words and a retry, rather than showing a
    /// result the store never produced (FR-018, FR-029). The retry is the step's ordinary reload, so the review comes
    /// back exactly as it was and the raise can be tried again.
    /// </remarks>
    protected override async Task<bool> PrepareToAdvanceAsync()
    {
        IsSubmitting = true;

        try
        {
            var submission = await _data.SubmitAsync(Draft);

            Draft.ResultPresentation = submission.IsRaised
                ? GenericRequestPresentation.Populated
                : GenericRequestPresentation.Unavailable;
            Draft.ResultAttemptedAtUtc = submission.AttemptedAtUtc;
            Draft.ResultMessage = submission.FailureMessage;
            Draft.RaisedRequestNumber = submission.RequestId;

            if (!submission.IsRaised)
            {
                AttemptedAtUtc = submission.AttemptedAtUtc;
                Presentation = GenericRequestPresentation.Unavailable;
            }

            return submission.IsRaised;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    protected override async Task LoadCoreAsync()
    {
        var availability = await _data.ReadStepAvailabilityAsync(Step);

        Rows.Clear();

        Add("GenericRequest_Summary_WorkCenter.Label", Draft.WorkCenter);
        Add("GenericRequest_Summary_Category.Label", Draft.Category);
        Add("GenericRequest_Summary_Item.Label", Draft.ItemName);
        Add("GenericRequest_Summary_Answer.Label", Draft.Answer);
        Add("GenericRequest_Summary_Part.Label", Draft.PartNumber);
        Add("GenericRequest_Summary_Stage.Label", Draft.PartStage == GenericRequestPartStage.Wip
            ? "GenericRequest_Part_Stage_Wip.Content".GetLocalized()
            : "GenericRequest_Part_Stage_FinishedGood.Content".GetLocalized());
        Add("GenericRequest_Summary_Sequence.Label", Draft.Sequence);
        Add("GenericRequest_Summary_Dunnage.Label", Join(Draft.DunnageType, Draft.DunnagePart));
        Add("GenericRequest_Summary_Scrap.Label", Draft.ScrapType);
        Add("GenericRequest_Summary_Die.Label", Draft.DieNumber);
        Add("GenericRequest_Summary_NeededBy.Label", Draft.NeededBy?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty);
        Add("GenericRequest_Summary_Estimate.Label", Draft.EstimatedMinutes is { } minutes
            ? minutes.ToString("0", CultureInfo.CurrentCulture)
            : string.Empty);
        Add("GenericRequest_Summary_Quantity.Label", Draft.PartNumber.Length == 0 ? string.Empty : Draft.Quantity.ToString("0.##", CultureInfo.CurrentCulture));
        Add("GenericRequest_Summary_Description.Label", Draft.Description);

        Apply(availability);
    }

    private void Add(string labelKey, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Rows.Add(new GenericRequestSummaryRow(labelKey.GetLocalized(), value));
        }
    }

    private static string Join(string first, string second) =>
        string.IsNullOrWhiteSpace(first) ? second
        : string.IsNullOrWhiteSpace(second) ? first
        : $"{first} / {second}";
}
