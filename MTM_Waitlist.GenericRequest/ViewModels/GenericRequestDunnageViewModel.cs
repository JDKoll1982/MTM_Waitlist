using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The dunnage step: choose a dunnage type from the catalogue, then the part that stands in for it.
/// </summary>
public sealed partial class GenericRequestDunnageViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;
    private IReadOnlyList<GenericRequestDunnageOption> _all = Array.Empty<GenericRequestDunnageOption>();

    public GenericRequestDunnageViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Dunnage;

    public override string TitleKey => "GenericRequest_Dunnage_Title.Text";

    public override string IntroKey => "GenericRequest_Dunnage_Intro.Text";

    public override string EmptyKey => "GenericRequest_Dunnage_Empty.Text";

    public ObservableCollection<string> Types { get; } = new();

    public ObservableCollection<GenericRequestDunnageOption> Parts { get; } = new();

    [ObservableProperty]
    public partial string? SelectedType { get; set; }

    [ObservableProperty]
    public partial GenericRequestDunnageOption? SelectedPart { get; set; }

    /// <summary>What is chosen right now, shown under the two lists.</summary>
    public string SelectionSummary => SelectedPart is { } part
        ? $"{part.TypeName} / {part.PartNumber}"
        : string.Empty;

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadDunnageAsync(Draft.ItemName);

        _all = result.Items;

        Types.Clear();
        foreach (var type in _all.Select(option => option.TypeName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Types.Add(type);
        }

        SelectedType = Types.FirstOrDefault(type => type == Draft.DunnageType) ?? Types.FirstOrDefault();

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        Draft.DunnageType = SelectedType ?? string.Empty;
        Draft.DunnagePart = SelectedPart?.PartNumber ?? string.Empty;
    }

    partial void OnSelectedTypeChanged(string? value) => ApplyTypeFilter();

    partial void OnSelectedPartChanged(GenericRequestDunnageOption? value) => OnPropertyChanged(nameof(SelectionSummary));

    private void ApplyTypeFilter()
    {
        Parts.Clear();

        foreach (var part in _all.Where(part => string.Equals(part.TypeName, SelectedType, StringComparison.OrdinalIgnoreCase)))
        {
            Parts.Add(part);
        }

        SelectedPart = Parts.FirstOrDefault(part => part.PartNumber == Draft.DunnagePart) ?? Parts.FirstOrDefault();
    }
}
