using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The work center step: pick the work center the request belongs to.</summary>
public sealed partial class GenericRequestWorkCenterViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestWorkCenterViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.WorkCenter;

    public override string TitleKey => "GenericRequest_WorkCenter_Title.Text";

    public override string IntroKey => "GenericRequest_WorkCenter_Intro.Text";

    public override string EmptyKey => "GenericRequest_WorkCenter_Empty.Text";

    public ObservableCollection<GenericRequestWorkCenterOption> Options { get; } = new();

    [ObservableProperty]
    public partial GenericRequestWorkCenterOption? Selected { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadWorkCentersAsync();

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.Name == Draft.WorkCenter);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is { } selected)
        {
            Draft.WorkCenter = selected.Name;
        }
    }
}
