using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;

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

    // FIX: A clean, type-safe string representation of the Enum for the XAML engine
    public string SelectedThemeText => ElementTheme.ToString();

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;

        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();
        UseRecvMockData = _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        UseInforVisualMockData = _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey).GetAwaiter().GetResult() ?? true;

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    // FIX: This partial method is automatically invoked by the MVVM Toolkit source generator 
    // whenever the ElementTheme property is modified, updating our custom XAML text field.
    partial void OnElementThemeChanged(ElementTheme value)
    {
        OnPropertyChanged(nameof(SelectedThemeText));
    }

    partial void OnUseRecvMockDataChanged(bool value)
    {
        _ = _localSettingsService.SaveSettingAsync(RecvMockDataSettingKey, value);
    }

    partial void OnUseInforVisualMockDataChanged(bool value)
    {
        _ = _localSettingsService.SaveSettingAsync(InforVisualMockDataSettingKey, value);
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
