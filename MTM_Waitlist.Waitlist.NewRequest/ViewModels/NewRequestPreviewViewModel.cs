using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Intermediate "request preview" step of the New Request wizard. Replaces the inline
/// <c>ShowRequestSummaryAsync</c> dialog and is shown only for request types that have
/// no subtype, letting the user review their selection before the final confirmation.
/// </summary>
public partial class NewRequestPreviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenter
    {
        get; set;
    } = string.Empty;

    /// <summary>The Category's umbrella word, in the new Category/Item vocabulary.</summary>
    [ObservableProperty]
    public partial string CategoryText
    {
        get; set;
    } = string.Empty;

    /// <summary>The Item the requester chose, in the new Category/Item vocabulary.</summary>
    [ObservableProperty]
    public partial string ItemText
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// One row per entry this run will raise — every die the operator chose, in the order they will be raised —
    /// rather than the request's single value, which showed one die while several were about to be raised (FR-054).
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> DetailLines
    {
        get; set;
    } = Array.Empty<string>();

    [ObservableProperty]
    public partial bool HasDetail
    {
        get; set;
    }

    public NewRequestPreviewViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenter = state.WorkCenter;
        CategoryText = NewRequestItemViewModel.ResolveCategoryName(state.Category ?? state.Item.Category);
        ItemText = NewRequestItemViewModel.ResolveItemName(state.Item);
        DetailLines = state.DetailLines();
        HasDetail = DetailLines.Count > 0;
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        _navigationService.NavigateTo(typeof(NewRequestSummaryViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
