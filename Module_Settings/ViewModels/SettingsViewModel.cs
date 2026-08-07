using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
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

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
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
    private bool _useRecvMockData;

    [ObservableProperty]
    private bool _useInforVisualMockData;

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

    public ObservableCollection<string> AvailableWorkstations { get; } = new();

    public ObservableCollection<string> HotWorkCenters { get; } = new();

    public ObservableCollection<string> OtherWorkCenters { get; } = new();

    public bool CanManageHotWorkCenters => AllowedHotWorkCenterManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

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
        StartupState startupState)
    {
        StartupDebugLog.Info("SettingsViewModel", "Constructor started.");
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _workCenterCatalogService = workCenterCatalogService;
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
        StartupDebugLog.Info("SettingsViewModel", $"Constructor completed. Theme='{ElementTheme}', Version='{VersionDescription}', RecvMockData={UseRecvMockData}, InforVisualMockData={UseInforVisualMockData}.");
    }

    // FIX: This partial method is automatically invoked by the MVVM Toolkit source generator 
    // whenever the ElementTheme property is modified, updating our custom XAML text field.
    partial void OnElementThemeChanged(ElementTheme value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"Theme changed to '{value}'.");
        OnPropertyChanged(nameof(SelectedThemeText));
    }

    partial void OnUseRecvMockDataChanged(bool value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"UseRecvMockData changed to {value}.");
        _ = _localSettingsService.SaveSettingAsync(RecvMockDataSettingKey, value);
    }

    partial void OnUseInforVisualMockDataChanged(bool value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"UseInforVisualMockData changed to {value}.");
        _ = _localSettingsService.SaveSettingAsync(InforVisualMockDataSettingKey, value);
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

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    private async Task InitializeHotWorkCentersAsync()
    {
        StartupDebugLog.Info("SettingsViewModel", "InitializeHotWorkCentersAsync started.");
        IsHotWorkCentersBusy = true;
        try
        {
            var workstations = await _workCenterCatalogService.GetAvailableWorkstationsAsync().ConfigureAwait(true);
            ReplaceCollectionValues(AvailableWorkstations, workstations);

            var currentWorkstation = _workCenterCatalogService.GetCurrentWorkstationName();
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
                ? "Hot workcenters saved."
                : saveMessage;

            StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', Message='{HotWorkCentersStatusMessage}'.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync failed. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            HotWorkCentersStatusMessage = $"Unable to save hot workcenters: {ex.Message}";
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
