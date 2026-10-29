using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The scrap type step: pick the scrap type the request needs.</summary>
public sealed partial class GenericRequestScrapTypeViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestScrapTypeViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Scrap;

    public override string TitleKey => "GenericRequest_Scrap_Title.Text";

    public override string IntroKey => "GenericRequest_Scrap_Intro.Text";

    public override string EmptyKey => "GenericRequest_Scrap_Empty.Text";

    public ObservableCollection<GenericRequestScrapOption> Options { get; } = new();

    [ObservableProperty]
    public partial GenericRequestScrapOption? Selected { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadScrapTypesAsync();

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.ScrapType == Draft.ScrapType);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is { } selected)
        {
            Draft.ScrapType = selected.ScrapType;
        }
    }
}
