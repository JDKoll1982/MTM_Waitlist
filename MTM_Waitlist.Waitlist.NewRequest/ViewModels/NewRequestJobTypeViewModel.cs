using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Category step of the New Request wizard — the second step, between the work centre and the Item. It binds the
/// Categories that would offer at least one Item to the requesting job, so a Category that would open an empty
/// Item step is never offered (FR-002, SC-005), and it resolves the job's availability snapshot here — between
/// the Category and the Item step — so the Item list is filtered before it is built (contract §3).
/// </summary>
/// <remarks>
/// The type and subtype stages are gone (FR-003): the step asks for an umbrella Category and nothing else, and
/// the wizard then asks for the Item on its own step.
/// </remarks>
public partial class NewRequestJobTypeViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly INewRequestFlowService _flowService;
    private readonly ICoilAvailabilityService _coilAvailabilityService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsLoadFailed
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsCoilBannerVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial string CoilBannerText
    {
        get; set;
    } = string.Empty;

    /// <summary>Prompt shown under the header.</summary>
    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = "Choose a request category to continue.";

    /// <summary>The Categories this job actually supports, in canonical order.</summary>
    public ObservableCollection<NewRequestPickerTile> Options { get; } = new();

    public NewRequestJobTypeViewModel(INavigationService navigationService, INewRequestFlowService flowService, ICoilAvailabilityService coilAvailabilityService)
    {
        _navigationService = navigationService;
        _flowService = flowService;
        _coilAvailabilityService = coilAvailabilityService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        state.Category = null;
        state.Item = null;
        state.ItemConfiguration = null;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        await LoadCategoriesAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Resolves the job's availability snapshot and binds the Categories that would offer at least one Item to
    /// it. The snapshot is resolved <b>here</b> — between the Category and the Item step — which is what makes the
    /// check run before the Item list is built rather than after an Item has been offered (FR-002, contract §3).
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        IsLoadFailed = false;
        try
        {
            var workCenter = _state?.WorkCenter?.Trim() ?? string.Empty;

            var availability = await _flowService.ResolveAvailabilityAsync(workCenter).ConfigureAwait(true);
            if (_state is not null)
            {
                _state.Availability = availability;
            }

            // Up-front coil readout: show the coil on the job (or a clear 'none') before the
            // worker chooses a category. Hide the banner when we have no meaningful state.
            var coil = await _coilAvailabilityService.GetCoilForJobAsync(workCenter).ConfigureAwait(true);
            var showCoil = coil.HasCoil && !string.IsNullOrWhiteSpace(coil.CoilNumber);
            var showNoCoil = !coil.HasCoil;
            IsCoilBannerVisible = showCoil || showNoCoil;
            CoilBannerText = showCoil
                ? $"Current coil {coil.CoilNumber} - {coil.Description} - {coil.QuantityOnHand} on hand - {coil.AverageWeight}"
                : showNoCoil ? $"No coil is loaded on this job." : string.Empty;

            Options.Clear();
            foreach (var category in _flowService.GetVisibleCategories(availability))
            {
                var itemCount = _flowService.GetVisibleItems(category, availability).Count;
                Options.Add(new NewRequestPickerTile
                {
                    Name = NewRequestItemViewModel.ResolveCategoryName(category),
                    Summary = itemCount == 1 ? "1 option" : $"{itemCount} options",
                    Category = category,
                });
            }

            StartupDebugLog.Info("NewRequestJobType", $"Category step for work center '{workCenter}' bound {Options.Count} category(ies).");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestJobType", ex, "Failed to load the request categories.");
            IsLoadFailed = true;
            Options.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Records the Category and moves to its Item step. Nothing is chosen on the requester's behalf: the Item is
    /// its own step, so an Item this job does not support can never be offered and then refused.
    /// </summary>
    [RelayCommand]
    private void SelectOption(NewRequestPickerTile? tile)
    {
        if (_state is null || tile?.Category is not { } category)
        {
            return;
        }

        _state.Category = category;
        _state.Item = null;
        _state.ItemConfiguration = null;
        _state.InputValue = null;

        StartupDebugLog.Info("NewRequestJobType", $"Selected category '{category}' for work center '{_state.WorkCenter}'.");
        _navigationService.NavigateTo(typeof(NewRequestItemViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();
}
