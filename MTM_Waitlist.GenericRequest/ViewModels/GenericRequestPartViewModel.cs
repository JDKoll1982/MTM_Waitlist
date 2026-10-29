using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>One entry in the part step's stage choice.</summary>
public sealed record GenericRequestStageOption(string Label, GenericRequestPartStage Stage);

/// <summary>
/// The part step: find the part, say whether it is a work in progress or a finished good, then choose its sequence.
/// </summary>
public sealed partial class GenericRequestPartViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;
    private IReadOnlyList<GenericRequestPartOption> _allParts = Array.Empty<GenericRequestPartOption>();

    public GenericRequestPartViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        StageOptions =
        [
            new("GenericRequest_Part_Stage_Wip.Content".GetLocalized(), GenericRequestPartStage.Wip),
            new("GenericRequest_Part_Stage_FinishedGood.Content".GetLocalized(), GenericRequestPartStage.FinishedGood),
        ];

        SelectedStage = StageOptions[0];
    }

    public override GenericRequestStep Step => GenericRequestStep.Part;

    public override string TitleKey => "GenericRequest_Part_Title.Text";

    public override string IntroKey => "GenericRequest_Part_Intro.Text";

    public override string EmptyKey => "GenericRequest_Part_Empty.Text";

    public IReadOnlyList<GenericRequestStageOption> StageOptions { get; }

    public ObservableCollection<GenericRequestPartOption> Parts { get; } = new();

    public ObservableCollection<GenericRequestSequenceOption> Sequences { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GenericRequestPartOption? SelectedPart { get; set; }

    [ObservableProperty]
    public partial GenericRequestStageOption? SelectedStage { get; set; }

    [ObservableProperty]
    public partial GenericRequestSequenceOption? SelectedSequence { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadPartsAsync(Draft.ItemName);

        _allParts = result.Items;
        ApplyFilter();
        SelectedPart = _allParts.FirstOrDefault(part => part.PartNumber == Draft.PartNumber);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (SelectedPart is { } part)
        {
            Draft.PartNumber = part.PartNumber;
        }

        if (SelectedStage is { } stage)
        {
            Draft.PartStage = stage.Stage;
        }

        if (SelectedSequence is { } sequence)
        {
            Draft.Sequence = sequence.Sequence;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedPartChanged(GenericRequestPartOption? value)
    {
        if (value is not null)
        {
            _ = LoadSequencesAsync(value.PartNumber);
        }
    }

    private void ApplyFilter()
    {
        Parts.Clear();

        foreach (var part in _allParts)
        {
            if (string.IsNullOrWhiteSpace(SearchText)
                || part.PartNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || part.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                Parts.Add(part);
            }
        }
    }

    private async Task LoadSequencesAsync(string partNumber)
    {
        var result = await _data.ReadSequencesAsync(partNumber);

        Sequences.Clear();
        foreach (var sequence in result.Items)
        {
            Sequences.Add(sequence);
        }

        SelectedSequence = Sequences.FirstOrDefault(sequence => sequence.Sequence == Draft.Sequence);
    }
}
