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
        if (Selected is not { } selected)
        {
            return;
        }

        if (string.Equals(Draft.WorkCenter, selected.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Choosing a different work centre makes every answer below it a statement about a work centre the request no
        // longer belongs to, so they are cleared rather than carried into a request the person did not describe.
        Draft.WorkCenter = selected.Name;
        Draft.Building = selected.Building;
        Draft.Category = string.Empty;
        Draft.ItemCode = string.Empty;
        Draft.ItemName = string.Empty;
        Draft.Answer = string.Empty;
        Draft.EstimatedMinutes = null;
        Draft.RequiresPart = false;
        Draft.RequiresDunnage = false;
        Draft.RequiresScrap = false;
        Draft.RequiresDie = false;

        // The stated scrap type says where one work centre's scrap goes, so it says nothing about another work
        // centre and is cleared with the rest rather than riding into a request nobody described.
        Draft.ScrapType = string.Empty;
    }
}
