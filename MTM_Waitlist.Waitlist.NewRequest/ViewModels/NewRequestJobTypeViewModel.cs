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
/// Second step of the New Request wizard. Re-laid (Phase 3) as a canonical Category→Item picker:
/// the worker first picks an umbrella Category (Pickup / Deliver / Assist / Other) then an Item
/// under it. Items are the canonical rows (Request-Config-Template.csv) grouped from the real DB
/// request-type tree by <see cref="NewRequestCanonicalPicker"/>, so selecting an Item submits its
/// resolved legacy leaf (RequestType + Subtype) and the downstream Details/Summary/persist steps are
/// unchanged. When no canonical categories can be built (DB empty/unreachable → default types carry no
/// Category/ItemId), the step falls back to the legacy request-type cards.
/// </summary>
public partial class NewRequestJobTypeViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly INewRequestFlowService _flowService;
    private readonly ICoilAvailabilityService _coilAvailabilityService;

    private NewRequestFlowState? _state;
    private IReadOnlyList<NewRequestTypeDefinition> _loadedRequestTypes = Array.Empty<NewRequestTypeDefinition>();
    private IReadOnlyList<NewRequestCanonicalCategory> _canonicalCategories = Array.Empty<NewRequestCanonicalCategory>();
    private bool _isShowingItems;

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

    /// <summary>Prompt shown under the header, switching between the category and item stages.</summary>
    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = "Choose a request category to continue.";

    /// <summary>True when the canonical Category→Item picker is active (vs. the legacy fallback).</summary>
    [ObservableProperty]
    public partial bool IsCanonicalPicker
    {
        get; set;
    }

    /// <summary>Current cards on the step: category tiles, item tiles, or legacy request-type tiles.</summary>
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
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        await LoadJobTypesAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadJobTypesAsync()
    {
        IsLoading = true;
        IsLoadFailed = false;
        try
        {
            var workCenter = _state?.WorkCenter?.Trim() ?? string.Empty;
            var coil = await _coilAvailabilityService.GetCoilForJobAsync(workCenter).ConfigureAwait(true);
            StartupDebugLog.Info("NewRequestJobType", $"Coil availability for work center '{workCenter}': HasCoil={coil.HasCoil}.");

            // Up-front coil readout: show the coil on the job (or a clear 'none') before the
            // worker chooses a request type. Hide the banner when we have no meaningful state.
            var showCoil = coil.HasCoil && !string.IsNullOrWhiteSpace(coil.CoilNumber);
            var showNoCoil = !coil.HasCoil;
            IsCoilBannerVisible = showCoil || showNoCoil;
            CoilBannerText = showCoil
                ? $"Current coil {coil.CoilNumber} - {coil.Description} - {coil.QuantityOnHand} on hand - {coil.AverageWeight}"
                : showNoCoil ? $"No coil is loaded on this job." : string.Empty;

            var requestTypes = NewRequestFlowRules.ApplyActiveJobEligibility(
                await _flowService.LoadRequestTypesAsync().ConfigureAwait(true),
                hasCoilData: coil.HasCoil,
                hasFlatstockData: true,
                hasPartData: true,
                hasWorkOrderData: true);

            _loadedRequestTypes = requestTypes;

            // Canonical Category→Item picker (Phase 3). Falls back to legacy request-type cards when
            // the loaded tree carries no canonical Category/ItemId (e.g. DB empty → default types).
            var canonical = NewRequestCanonicalPicker.BuildPicker(requestTypes);
            _canonicalCategories = canonical;
            IsCanonicalPicker = canonical.Count > 0;
            PromptText = canonical.Count > 0
                ? "Choose a request category to continue."
                : "Choose a job type to continue.";
            _isShowingItems = false;
            await PopulateCurrentStageAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("NewRequestJobType", ex, "Failed to load job types.");
            IsLoadFailed = true;
            Options.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Repopulates the grid for the current stage (categories or legacy types).</summary>
    private async Task PopulateCurrentStageAsync()
    {
        Options.Clear();

        if (IsCanonicalPicker)
        {
            foreach (var category in _canonicalCategories)
            {
                Options.Add(new NewRequestPickerTile
                {
                    Name = category.DisplayName,
                    Summary = category.Items.Count == 1 ? "1 option" : $"{category.Items.Count} options",
                    Category = category.Category,
                });
            }

            return;
        }

        foreach (var requestType in _loadedRequestTypes.Where(item => !string.IsNullOrWhiteSpace(item.RequestType)))
        {
            var requestTypeName = requestType.RequestType.Trim();
            Options.Add(new NewRequestPickerTile
            {
                Name = requestTypeName,
                Summary = requestType.Subtypes.Count == 0
                    ? "No subtype required"
                    : $"{requestType.Subtypes.Count} subtype option(s)",
                ImagePath = await _flowService.ResolveRequestTypeImagePathAsync(requestTypeName).ConfigureAwait(true),
                RequestType = requestType,
            });
        }
    }

    [RelayCommand]
    private async Task SelectOptionAsync(NewRequestPickerTile? tile)
    {
        if (_state is null || tile is null)
        {
            return;
        }

        if (tile.Category is { } category)
        {
            await EnterCategoryStageAsync(category).ConfigureAwait(true);
            return;
        }

        if (tile.Item is { } item)
        {
            SubmitCanonicalItem(item);
            return;
        }

        if (tile.RequestType is { } requestType)
        {
            SubmitLegacyType(requestType);
        }
    }

    private async Task EnterCategoryStageAsync(RequestCategory category)
    {
        var categoryGroup = _canonicalCategories.FirstOrDefault(candidate => candidate.Category == category);
        if (categoryGroup is null || categoryGroup.Items.Count == 0)
        {
            return;
        }

        _isShowingItems = true;
        PromptText = $"{categoryGroup.DisplayName} — choose an item to continue.";
        Options.Clear();
        foreach (var item in categoryGroup.Items)
        {
            Options.Add(new NewRequestPickerTile
            {
                Name = item.DisplayName,
                Summary = item.Summary,
                ImagePath = await ResolveCanonicalItemImageAsync(item).ConfigureAwait(true),
                Item = item,
            });
        }
    }

    private async Task<string> ResolveCanonicalItemImageAsync(NewRequestCanonicalItem item)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(item.Subtype))
            {
                return await _flowService
                    .ResolveRequestSubtypeImagePathAsync(item.RequestType, item.Subtype)
                    .ConfigureAwait(true);
            }

            return await _flowService.ResolveRequestTypeImagePathAsync(item.RequestType).ConfigureAwait(true);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>Submits a canonical Item by applying its resolved legacy leaf to the wizard state.</summary>
    private void SubmitCanonicalItem(NewRequestCanonicalItem item)
    {
        if (_state is null)
        {
            return;
        }

        var requestType = _loadedRequestTypes.FirstOrDefault(candidate =>
            string.Equals(candidate.RequestType.Trim(), item.RequestType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (requestType is null)
        {
            StartupDebugLog.Info("NewRequestJobType", $"No request type '{item.RequestType}' for item '{item.ItemId}'; ignoring selection.");
            return;
        }

        NewRequestSubtypeDefinition? subtype = null;
        if (!string.IsNullOrWhiteSpace(item.Subtype))
        {
            subtype = requestType.Subtypes.FirstOrDefault(candidate =>
                string.Equals(candidate.Name.Trim(), item.Subtype!.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        _state.RequestType = requestType;
        _state.Subtype = subtype;
        _state.InputValue = null;

        StartupDebugLog.Info("NewRequestJobType", $"Selected canonical item '{item.ItemId}' -> '{requestType.RequestType}' / '{subtype?.Name ?? "(type leaf)"}' for work center '{_state.WorkCenter}'.");
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(_state).FullName!, _state);
    }

    /// <summary>Legacy fallback: the worker picked a request type card (DB-down path).</summary>
    private void SubmitLegacyType(NewRequestTypeDefinition selectedRequestType)
    {
        if (_state is null)
        {
            return;
        }

        _state.RequestType = selectedRequestType;
        _state.Subtype = null;
        _state.InputValue = null;

        StartupDebugLog.Info("NewRequestJobType", $"Selected job type '{selectedRequestType.RequestType}' for work center '{_state.WorkCenter}'.");

        if (selectedRequestType.Subtypes.Count > 0)
        {
            _navigationService.NavigateTo(typeof(NewRequestSubtypeViewModel).FullName!, _state);
            return;
        }

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(_state).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        // In the item sub-stage, Back returns to the category list instead of leaving the step.
        if (_isShowingItems)
        {
            _isShowingItems = false;
            PromptText = "Choose a request category to continue.";
            Options.Clear();
            foreach (var category in _canonicalCategories)
            {
                Options.Add(new NewRequestPickerTile
                {
                    Name = category.DisplayName,
                    Summary = category.Items.Count == 1 ? "1 option" : $"{category.Items.Count} options",
                    Category = category.Category,
                });
            }

            return;
        }

        _navigationService.GoBack();
    }
}
