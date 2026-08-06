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

    public Visibility ConfirmButtonVisibility => State.RequiresReplacementConfirmation ? Visibility.Collapsed : Visibility.Visible;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupSubordinatePart> SubordinateParts => State.SubordinateParts;

    public ObservableCollection<SetupDunnagePart> DunnagePairAssignments => State.SelectedDunnageParts;

    public IReadOnlyList<SetupDunnageAssignmentDisplay> DunnageAssignmentDisplays => State.SelectedDunnageParts
        .GroupBy(part => string.IsNullOrWhiteSpace(part.PartNumber) ? part.DisplayName : part.PartNumber, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .Select(part => new SetupDunnageAssignmentDisplay
        {
            PartNumber = part.PartNumber,
            DisplayName = string.IsNullOrWhiteSpace(part.DisplayName) ? part.PartNumber : part.DisplayName,
            IconGlyph = ResolveTypeIconGlyph(part.TypeId)
        })
        .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

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
        OnPropertyChanged(nameof(ConfirmButtonVisibility));
        OnPropertyChanged(nameof(SelectedDunnageSummary));
        OnPropertyChanged(nameof(DunnageAssignmentDisplays));
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
            OnPropertyChanged(nameof(ConfirmButtonVisibility));

            if (result.RequiresReplacementConfirmation)
            {
                return;
            }

            State.StatusMessage = result.Message;
            _navigationService.NavigateTo(
                typeof(SetupCompletionViewModel).FullName!,
                new SetupCompletionNavigationData
                {
                    Success = result.Success,
                    Message = result.Message,
                });
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
            OnPropertyChanged(nameof(ConfirmButtonVisibility));
            State.StatusMessage = result.Message;
            _navigationService.NavigateTo(
                typeof(SetupCompletionViewModel).FullName!,
                new SetupCompletionNavigationData
                {
                    Success = result.Success,
                    Message = result.Message,
                });
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
        OnPropertyChanged(nameof(ConfirmButtonVisibility));
    }

    private string ResolveTypeIconGlyph(string typeId)
    {
        var type = State.DunnageTypes.FirstOrDefault(candidate => string.Equals(candidate.Id, typeId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(type?.IconGlyph) ? "\uE8B7" : type.IconGlyph;
    }
}

public sealed class SetupDunnageAssignmentDisplay
{
    public string DisplayName { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string IconGlyph { get; set; } = "\uE8B7";
}