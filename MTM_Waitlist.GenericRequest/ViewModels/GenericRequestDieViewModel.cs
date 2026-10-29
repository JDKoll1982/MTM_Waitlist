using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The die step: search, then pick the die the request needs.</summary>
public sealed partial class GenericRequestDieViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestDieViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        SearchCommand = new AsyncRelayCommand(LoadAsync);
    }

    public override GenericRequestStep Step => GenericRequestStep.Die;

    public override string TitleKey => "GenericRequest_Die_Title.Text";

    public override string IntroKey => "GenericRequest_Die_Intro.Text";

    public override string EmptyKey => "GenericRequest_Die_Empty.Text";

    /// <summary>Run the search again with the text as typed.</summary>
    public IAsyncRelayCommand SearchCommand { get; }

    public ObservableCollection<GenericRequestDieOption> Options { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GenericRequestDieOption? Selected { get; set; }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadDiesAsync(SearchText);

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option => option.DieNumber == Draft.DieNumber);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is { } selected)
        {
            Draft.DieNumber = selected.DieNumber;
        }
    }
}
