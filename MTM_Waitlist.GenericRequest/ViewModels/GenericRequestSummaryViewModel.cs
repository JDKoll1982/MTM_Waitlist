using System.Collections.ObjectModel;
using System.Globalization;

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

    protected override async Task LoadCoreAsync()
    {
        var availability = await _data.ReadStepAvailabilityAsync(Step);

        Rows.Clear();

        Add("GenericRequest_Summary_WorkCenter.Label", Draft.WorkCenter);
        Add("GenericRequest_Summary_Category.Label", Draft.Category);
        Add("GenericRequest_Summary_Item.Label", Draft.ItemName);
        Add("GenericRequest_Summary_Part.Label", Draft.PartNumber);
        Add("GenericRequest_Summary_Stage.Label", Draft.PartStage == GenericRequestPartStage.Wip
            ? "GenericRequest_Part_Stage_Wip.Content".GetLocalized()
            : "GenericRequest_Part_Stage_FinishedGood.Content".GetLocalized());
        Add("GenericRequest_Summary_Sequence.Label", Draft.Sequence);
        Add("GenericRequest_Summary_Dunnage.Label", Join(Draft.DunnageType, Draft.DunnagePart));
        Add("GenericRequest_Summary_Scrap.Label", Draft.ScrapType);
        Add("GenericRequest_Summary_Die.Label", Draft.DieNumber);
        Add("GenericRequest_Summary_NeededBy.Label", Draft.NeededBy?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty);
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
