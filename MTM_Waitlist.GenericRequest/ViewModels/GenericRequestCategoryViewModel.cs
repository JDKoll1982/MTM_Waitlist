using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The category step: pick the category the item list is narrowed to.</summary>
public sealed partial class GenericRequestCategoryViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestCategoryViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Category;

    public override string TitleKey => "GenericRequest_Category_Title.Text";

    public override string IntroKey => "GenericRequest_Category_Intro.Text";

    public override string EmptyKey => "GenericRequest_Category_Empty.Text";

    public ObservableCollection<GenericRequestCategoryOption> Options { get; } = new();

    [ObservableProperty]
    public partial GenericRequestCategoryOption? Selected { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadCategoriesAsync(Draft.WorkCenter);

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.Name == Draft.Category);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is { } selected)
        {
            Draft.Category = selected.Name;
        }
    }
}
