using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The item step: pick the item the request is about, which is what says which extra steps apply.</summary>
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

    [ObservableProperty]
    public partial GenericRequestItemOption? Selected { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadItemsAsync(Draft.Category);

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.Name == Draft.ItemName);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is not { } selected)
        {
            return;
        }

        Draft.ItemName = selected.Name;
        Draft.RequiresDunnage = selected.NeedsDunnage;
        Draft.RequiresScrap = selected.NeedsScrap;
        Draft.RequiresDie = selected.NeedsDie;
    }
}
