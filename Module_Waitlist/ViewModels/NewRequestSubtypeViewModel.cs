using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Third step of the New Request wizard. Replaces the dialog-era
/// <c>NewRequestSubtypeDialog</c>: the user picks a subtype for the selected job type.
/// </summary>
public partial class NewRequestSubtypeViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly INewRequestFlowService _flowService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string RequestTypeName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading
    {
        get; set;
    }

    public ObservableCollection<NewRequestOptionItem> Subtypes { get; } = new();

    public NewRequestSubtypeViewModel(INavigationService navigationService, INewRequestFlowService flowService)
    {
        _navigationService = navigationService;
        _flowService = flowService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state
            || state.RequestType is null
            || state.RequestType.Subtypes.Count == 0)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        RequestTypeName = state.RequestType.RequestType.Trim();
        await LoadSubtypesAsync(state.RequestType).ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadSubtypesAsync(NewRequestTypeDefinition requestType)
    {
        IsLoading = true;
        try
        {
            Subtypes.Clear();
            foreach (var subtype in requestType.Subtypes)
            {
                var subtypeName = subtype.Name.Trim();
                Subtypes.Add(new NewRequestOptionItem
                {
                    Name = subtypeName,
                    Summary = subtypeName,
                    ImagePath = await _flowService
                        .ResolveRequestSubtypeImagePathAsync(requestType.RequestType, subtypeName)
                        .ConfigureAwait(true),
                });
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestSubtype", ex, "Failed to load subtypes.");
            Subtypes.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectSubtype(NewRequestOptionItem? item)
    {
        if (_state is null
            || _state.RequestType is null
            || item is null
            || string.IsNullOrWhiteSpace(item.Name))
        {
            return;
        }

        var selectedSubtype = _state.RequestType.Subtypes.FirstOrDefault(subtype =>
            string.Equals(subtype.Name.Trim(), item.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (selectedSubtype is null)
        {
            return;
        }

        _state.Subtype = selectedSubtype;
        _state.InputValue = null;

        StartupDebugLog.Info("NewRequestSubtype", $"Selected subtype '{selectedSubtype.Name}' for request type '{_state.RequestType.RequestType}'.");
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(_state).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
