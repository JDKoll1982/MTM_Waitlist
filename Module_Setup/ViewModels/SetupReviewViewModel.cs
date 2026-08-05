using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupReviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaving
    {
        get; set;
    }

    public Visibility ReplacementConfirmationVisibility => State.RequiresReplacementConfirmation ? Visibility.Visible : Visibility.Collapsed;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupSubordinatePart> SubordinateParts => State.SubordinateParts;

    public IReadOnlyList<SetupSubordinatePartGroup> SubordinatePartGroups => State.SubordinateParts
        .GroupBy(part => string.IsNullOrWhiteSpace(part.Category) ? "Other" : part.Category)
        .OrderBy(group => group.Key)
        .Select(group => new SetupSubordinatePartGroup
        {
            Category = group.Key,
            Parts = group.ToArray()
        })
        .ToArray();

    public string PageTitle => "Setup_Review.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step5".GetLocalized();

    public string SelectedDunnageSummary => State.SelectedDunnageSummary;

    public SetupReviewViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(SubordinatePartGroups));
        OnPropertyChanged(nameof(ReplacementConfirmationVisibility));
        OnPropertyChanged(nameof(SelectedDunnageSummary));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Edit()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        IsSaving = true;
        try
        {
            var result = await _workflowService.SaveAsync(false).ConfigureAwait(true);
            StatusMessage = result.Message;
            State.RequiresReplacementConfirmation = result.RequiresReplacementConfirmation;
            OnPropertyChanged(nameof(ReplacementConfirmationVisibility));

            if (result.RequiresReplacementConfirmation)
            {
                return;
            }

            State.StatusMessage = result.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ReplaceAsync()
    {
        IsSaving = true;
        try
        {
            var result = await _workflowService.SaveAsync(true).ConfigureAwait(true);
            StatusMessage = result.Message;
            State.RequiresReplacementConfirmation = false;
            OnPropertyChanged(nameof(ReplacementConfirmationVisibility));
            State.StatusMessage = result.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void CancelReplacement()
    {
        State.RequiresReplacementConfirmation = false;
        OnPropertyChanged(nameof(ReplacementConfirmationVisibility));
    }
}