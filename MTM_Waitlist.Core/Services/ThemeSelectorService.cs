using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IThemeSelectorService"/>
/// <remarks>
/// The chosen theme is held against the person in the store rather than in this computer's settings file, so
/// signing in on a different computer brings the same theme with it (FR-023, SC-008). It is a reading preference
/// and it belongs to the person: two operators sharing a desk should not have to agree about it.
/// </remarks>
public class ThemeSelectorService : IThemeSelectorService
{
    /// <summary>The key the chosen background theme is stored under, against the person.</summary>
    public const string SettingsKey = "AppBackgroundRequestedTheme";

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    private readonly IScopedPreferenceStore _preferences;
    private readonly IAppWindowProvider _appWindowProvider;

    public ThemeSelectorService(IScopedPreferenceStore preferences, IAppWindowProvider appWindowProvider)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(appWindowProvider);

        _preferences = preferences;
        _appWindowProvider = appWindowProvider;
    }

    public async Task InitializeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync();
        await SaveThemeInSettingsAsync(Theme);
    }

    public async Task SetRequestedThemeAsync()
    {
        if (_appWindowProvider.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(_appWindowProvider.MainWindow, Theme);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _preferences
            .ReadTextAsync(SettingsKey, PreferenceScope.Person)
            .ConfigureAwait(false);

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        try
        {
            await _preferences
                .WriteTextAsync(SettingsKey, PreferenceScope.Person, theme.ToString())
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The theme has already been applied to the open window, so a store that refuses the write costs the
            // person the choice at the next sign-in rather than the change they just made.
            AppLog.Error("Theme", ex, "The chosen theme could not be stored; it applies to this session only.");
        }
    }
}
