using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";
    private static readonly string[] AllowedHotWorkCenterManageRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private static readonly string[] AllowedImageLocationManageRoles =
    {
        "Admin",
        "Developer",
    };

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IDunnageTypeVisibilityCatalogService _dunnageTypeVisibilityCatalogService;
    private readonly StartupState _startupState;

    [ObservableProperty]
    public partial ElementTheme ElementTheme
    {
        get; set;
    }

    [ObservableProperty]
    public partial string VersionDescription
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool UseRecvMockData
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool UseInforVisualMockData
    {
        get; set;
    }

    [ObservableProperty]
    public partial string SearchQuery
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string SelectedWorkstation
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsHotWorkCentersBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string HotWorkCentersStatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsDunnageTypeVisibilityBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string DunnageTypeVisibilityStatusMessage
    {
        get; set;
    } = string.Empty;

    public ObservableCollection<string> AvailableWorkstations { get; } = new();

    public ObservableCollection<string> HotWorkCenters { get; } = new();

    public ObservableCollection<string> OtherWorkCenters { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> VisibleDunnageTypes { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> HiddenDunnageTypes { get; } = new();

    public bool CanManageHotWorkCenters => AllowedHotWorkCenterManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public bool CanManageImageLocationSettings => AllowedImageLocationManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public bool CanManageDunnageTypeVisibility => CanManageHotWorkCenters;

    public bool IsAppearancePanelVisible => MatchesSearch("appearance", "app theme", "light", "dark", "default", SelectedThemeText);

    public bool IsMockDataPanelVisible => MatchesSearch("mock data", "infor visual", "receiving", "mysql", "sample data");

    public bool IsHotWorkCentersPanelVisible => MatchesSearch(
        "Local Work Centers",
        "workstation",
        "computer",
        string.Join(" ", HotWorkCenters),
        string.Join(" ", OtherWorkCenters),
        string.Join(" ", AvailableWorkstations));

    public bool IsDunnageTypeVisibilityPanelVisible => MatchesSearch(
        "dunnage",
        "visibility",
        "shown",
        "hidden",
        string.Join(" ", VisibleDunnageTypes.Select(item => item.Name)),
        string.Join(" ", HiddenDunnageTypes.Select(item => item.Name)));

    public bool IsAboutPanelVisible => MatchesSearch("about", "version", "privacy", VersionDescription, "mtm waitlist");

    public bool IsAppearanceCategoryVisible => IsAppearancePanelVisible;

    public bool IsImageLocationSettingsPanelVisible => CanManageImageLocationSettings && MatchesSearch(
        "image location",
        "image settings",
        "request type images",
        "work center images",
        "subtype images",
        "image path",
        "request type",
        "subtype",
        "work center");

    public bool IsOperationsCategoryVisible => IsMockDataPanelVisible || IsHotWorkCentersPanelVisible || IsDunnageTypeVisibilityPanelVisible || IsImageLocationSettingsPanelVisible;

    public bool IsAboutCategoryVisible => IsAboutPanelVisible;

    // FIX: A clean, type-safe string representation of the Enum for the XAML engine
    public string SelectedThemeText => ElementTheme.ToString();

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(
        IThemeSelectorService themeSelectorService,
        ILocalSettingsService localSettingsService,
        IWorkCenterCatalogService workCenterCatalogService,
        IDunnageTypeVisibilityCatalogService dunnageTypeVisibilityCatalogService,
        StartupState startupState)
    {
        StartupDebugLog.Info("SettingsViewModel", "Constructor started.");
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _workCenterCatalogService = workCenterCatalogService;
        _dunnageTypeVisibilityCatalogService = dunnageTypeVisibilityCatalogService;
        _startupState = startupState;

        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();
        UseRecvMockData = _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        UseInforVisualMockData = _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey).GetAwaiter().GetResult() ?? false;

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        _ = InitializeHotWorkCentersAsync();
        _ = InitializeDunnageTypeVisibilityAsync();
        RefreshSearchVisibility();
        StartupDebugLog.Info("SettingsViewModel", $"Constructor completed. Theme='{ElementTheme}', Version='{VersionDescription}', RecvMockData={UseRecvMockData}, InforVisualMockData={UseInforVisualMockData}.");
    }

    // FIX: This partial method is automatically invoked by the MVVM Toolkit source generator 
    // whenever the ElementTheme property is modified, updating our custom XAML text field.
    partial void OnElementThemeChanged(ElementTheme value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"Theme changed to '{value}'.");
        OnPropertyChanged(nameof(SelectedThemeText));
        RefreshSearchVisibility();
    }

    partial void OnUseRecvMockDataChanged(bool value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"UseRecvMockData changed to {value}.");
        _ = _localSettingsService.SaveSettingAsync(RecvMockDataSettingKey, value);
        RefreshSearchVisibility();
    }

    partial void OnUseInforVisualMockDataChanged(bool value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"UseInforVisualMockData changed to {value}.");
        _ = _localSettingsService.SaveSettingAsync(InforVisualMockDataSettingKey, value);
        RefreshSearchVisibility();
    }

    partial void OnSearchQueryChanged(string value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"SearchQuery changed to '{value}'.");
        RefreshSearchVisibility();
    }

    partial void OnSelectedWorkstationChanged(string value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'.");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _ = LoadCatalogForWorkstationAsync(value);
    }

    [RelayCommand]
    private async Task AddHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        if (HotWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync skipped because '{normalizedWorkCenter}' is already pinned.");
            return;
        }

        var existingOthers = OtherWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        HotWorkCenters.Add(normalizedWorkCenter);
        SortCollection(HotWorkCenters);
        ReplaceCollectionValues(OtherWorkCenters, existingOthers);
        RefreshSearchVisibility();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        StartupDebugLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        var updatedHot = HotWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        ReplaceCollectionValues(HotWorkCenters, updatedHot);

        if (!OtherWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            OtherWorkCenters.Add(normalizedWorkCenter);
            SortCollection(OtherWorkCenters);
        }

        RefreshSearchVisibility();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ShowDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = HiddenDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        StartupDebugLog.Info("SettingsDunnageVisibility", $"ShowDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextHidden = HiddenDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextVisible = VisibleDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task HideDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = VisibleDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        StartupDebugLog.Info("SettingsDunnageVisibility", $"HideDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextVisible = VisibleDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextHidden = HiddenDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    private async Task InitializeHotWorkCentersAsync()
    {
        StartupDebugLog.Info("SettingsViewModel", "InitializeHotWorkCentersAsync started.");
        IsHotWorkCentersBusy = true;
        try
        {
            var workstations = await _workCenterCatalogService.GetAvailableComputersAsync().ConfigureAwait(true);
            ReplaceCollectionValues(AvailableWorkstations, workstations);

            var currentWorkstation = _workCenterCatalogService.GetCurrentComputerName();
            var resolvedWorkstation = AvailableWorkstations.FirstOrDefault(item =>
                                          string.Equals(item, currentWorkstation, StringComparison.OrdinalIgnoreCase))
                ?? AvailableWorkstations.FirstOrDefault()
                ?? currentWorkstation;

            var workstationChanged = !string.Equals(SelectedWorkstation, resolvedWorkstation, StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(SelectedWorkstation))
            {
                SelectedWorkstation = resolvedWorkstation;
            }
            else if (workstationChanged)
            {
                SelectedWorkstation = resolvedWorkstation;
            }
            else
            {
                await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true);
            }

            RefreshSearchVisibility();

            StartupDebugLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', AvailableCount={AvailableWorkstations.Count}.");
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task LoadCatalogForWorkstationAsync(string workstationName)
    {
        StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workstation='{workstationName}'.");
        IsHotWorkCentersBusy = true;
        try
        {
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            ReplaceCollectionValues(HotWorkCenters, catalog.HotWorkCenters);
            ReplaceCollectionValues(OtherWorkCenters, catalog.OtherWorkCenters);
            HotWorkCentersStatusMessage = string.Empty;
            RefreshSearchVisibility();
            StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Workstation='{workstationName}', HotCount={HotWorkCenters.Count}, OtherCount={OtherWorkCenters.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Workstation='{workstationName}'.");
            HotWorkCentersStatusMessage = $"Unable to load work centers: {ex.Message}";
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task SaveCurrentHotWorkCentersAsync()
    {
        try
        {
            StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            var saveMessage = await _workCenterCatalogService
                .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray())
                .ConfigureAwait(true);

            HotWorkCentersStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Local workcenters saved."
                : saveMessage;

            StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', Message='{HotWorkCentersStatusMessage}'.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync failed. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            HotWorkCentersStatusMessage = $"Unable to save Local workcenters: {ex.Message}";
        }
    }

    private async Task InitializeDunnageTypeVisibilityAsync()
    {
        StartupDebugLog.Info("SettingsDunnageVisibility", "InitializeDunnageTypeVisibilityAsync started.");
        IsDunnageTypeVisibilityBusy = true;
        try
        {
            var catalog = await _dunnageTypeVisibilityCatalogService.GetCatalogAsync().ConfigureAwait(true);
            ReplaceDunnageTypeValues(VisibleDunnageTypes, catalog.VisibleDunnageTypes);
            ReplaceDunnageTypeValues(HiddenDunnageTypes, catalog.HiddenDunnageTypes);
            DunnageTypeVisibilityStatusMessage = string.Empty;
            RefreshSearchVisibility();
            StartupDebugLog.Info("SettingsDunnageVisibility", $"InitializeDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsDunnageVisibility", ex, "InitializeDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to load dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }

    private async Task SaveCurrentDunnageTypeVisibilityAsync()
    {
        try
        {
            IsDunnageTypeVisibilityBusy = true;
            var saveMessage = await _dunnageTypeVisibilityCatalogService
                .SaveVisibleDunnageTypesAsync(VisibleDunnageTypes.Select(item => item.Id).ToArray())
                .ConfigureAwait(true);

            DunnageTypeVisibilityStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Dunnage visibility saved."
                : saveMessage;

            StartupDebugLog.Info("SettingsDunnageVisibility", $"SaveCurrentDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}, Message='{DunnageTypeVisibilityStatusMessage}'.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsDunnageVisibility", ex, "SaveCurrentDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to save dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }

    private static void ReplaceCollectionValues(ObservableCollection<string> targetCollection, IEnumerable<string> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(value => value.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }

    private static void SortCollection(ObservableCollection<string> values)
    {
        var sorted = values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        values.Clear();
        foreach (var value in sorted)
        {
            values.Add(value);
        }
    }

    private static void ReplaceDunnageTypeValues(ObservableCollection<DunnageTypeVisibilityOption> targetCollection, IEnumerable<DunnageTypeVisibilityOption> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
                     .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First())
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }

    private bool MatchesSearch(params string[] values)
    {
        var query = SearchQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshSearchVisibility()
    {
        OnPropertyChanged(nameof(IsAppearancePanelVisible));
        OnPropertyChanged(nameof(IsMockDataPanelVisible));
        OnPropertyChanged(nameof(IsHotWorkCentersPanelVisible));
        OnPropertyChanged(nameof(IsDunnageTypeVisibilityPanelVisible));
        OnPropertyChanged(nameof(IsAboutPanelVisible));
        OnPropertyChanged(nameof(IsAppearanceCategoryVisible));
        OnPropertyChanged(nameof(IsOperationsCategoryVisible));
        OnPropertyChanged(nameof(IsAboutCategoryVisible));
    }

    private static string GetVersionDescription()
    {
        Version version;
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
