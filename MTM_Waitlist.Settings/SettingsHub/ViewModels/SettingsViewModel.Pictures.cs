using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// Asks the on-host service to rebuild the cached Infor Visual reads now (T160).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This does not make the application refresh its own cache - the on-host service still owns the schedule
    /// and the write (FR-025). It asks that service to run one cycle early, and the same request can be made
    /// from the host with `POST /api/refresh`.
    /// </para>
    /// <para>
    /// Every failure path is a reported outcome, never an exception: an absent service, an unconfigured
    /// client and a refusal all land in <see cref="CacheRefreshStatusMessage"/> and leave the application
    /// serving cached content (FR-025, SC-011).
    /// </para>
    /// </remarks>
    /// <summary>
    /// Reads the picture cache's stored settings.
    /// </summary>
    /// <remarks>
    /// Started from the constructor and awaited by nothing, like the other panels on this screen: the page must not
    /// block on a store read, and the shipped defaults answer until the real values arrive. A read that fails
    /// leaves those defaults in place and is recorded rather than shown.
    /// </remarks>
    private async Task InitializePictureCacheAsync()
    {
        if (_imageStorageConfigurationResolver is null)
        {
            _pictureCacheInitializing = false;
            return;
        }

        try
        {
            PictureCacheFolderPath = await _imageStorageConfigurationResolver
                .GetImageCacheFolderPathAsync()
                .ConfigureAwait(true);
            PictureCacheFolderInput = PictureCacheFolderPath;

            IsPictureCacheEnabled = await _imageStorageConfigurationResolver
                .GetImageCacheEnabledAsync()
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "SettingsPictureCache",
                ex,
                "The picture cache's settings could not be read; the shipped defaults are shown and the store will be asked again next visit.");
        }
        finally
        {
            _pictureCacheInitializing = false;
            RefreshSearchVisibility();
        }
    }

    partial void OnIsPictureCacheEnabledChanged(bool value)
    {
        if (_pictureCacheInitializing)
        {
            return;
        }

        _ = SavePictureCacheEnabledAsync(value);
    }

    /// <summary>
    /// Stores the picture cache toggle for everybody, then says what will happen next.
    /// </summary>
    /// <remarks>
    /// The copy itself is not started here: switching it off should not mean "stop half way", and switching it on
    /// is what the next start does anyway. The message says "next time" for that reason.
    /// </remarks>
    private async Task SavePictureCacheEnabledAsync(bool value)
    {
        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageCacheEnabled,
                "bool",
                text: null,
                boolean: value).ConfigureAwait(true);

            PictureCacheStatusMessage = value
                ? "Saved for every computer. Pictures will be copied onto each one the next time the application starts."
                : "Saved for every computer. Pictures will be read from the network the next time the application starts.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to save that change.";
            AppLog.Error("SettingsPictureCache", ex, "The picture cache toggle could not be stored.");
        }
    }

    /// <summary>Stores the cache folder that was typed into the box, for everybody.</summary>
    [RelayCommand]
    private async Task SavePictureCacheFolderAsync()
    {
        if (_configSettingsValueService is null || _imageStorageConfigurationResolver is null)
        {
            return;
        }

        var folder = PictureCacheFolderInput?.Trim() ?? string.Empty;

        if (folder.Length == 0)
        {
            PictureCacheStatusMessage = "A folder is needed before this can be saved.";
            return;
        }

        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageCacheFolderPath,
                "text",
                folder,
                boolean: null).ConfigureAwait(true);

            PictureCacheFolderPath = folder;
            PictureCacheStatusMessage = $"Saved for every computer. Pictures will be cached in {folder}.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to save that folder.";
            AppLog.Error("SettingsPictureCache", ex, "The picture cache folder could not be stored.");
        }
    }


    /// <summary>
    /// Copies the pictures onto this computer now, without waiting for the next start.
    /// </summary>
    /// <remarks>
    /// The same synchronisation the startup step runs, so the two can never disagree about what a cached picture
    /// is. What it did is reported as counts, because a folder that turned out to be empty and a copy that did
    /// nothing look identical otherwise.
    /// </remarks>
    [RelayCommand]
    private async Task RefreshPictureCacheAsync()
    {
        if (_imageCacheSyncService is null || IsPictureCacheBusy)
        {
            return;
        }

        IsPictureCacheBusy = true;
        PictureCacheStatusMessage = "Copying pictures...";

        try
        {
            var result = await _imageCacheSyncService.SynchronizeAsync().ConfigureAwait(true);

            PictureCacheStatusMessage = result.DestinationsUnavailable.Count > 0
                ? $"Copied {result.Copied}, removed {result.Removed}. Nothing was done for {string.Join(", ", result.DestinationsUnavailable)}, because the folder the copies go into could not be written to."
                : result.SourcesSkipped.Count > 0
                    ? $"Copied {result.Copied}, removed {result.Removed}. Nothing was done for {string.Join(", ", result.SourcesSkipped)}, which could not be reached."
                    : result.NotCopied > 0
                        ? $"Copied {result.Copied}, removed {result.Removed}. {result.NotCopied} picture(s) could not be copied."
                        : $"Copied {result.Copied}, removed {result.Removed}.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to copy the pictures.";
            AppLog.Error("SettingsPictureCache", ex, "The picture copy requested from the Settings screen failed.");
        }
        finally
        {
            IsPictureCacheBusy = false;
        }
    }

    /// <summary>
    /// Opens the folder dialog at the hopper picture folder and puts the folder the person chose in the box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dialog starts at what the box already holds, and at the shipped share folder when the box is empty, so
    /// picking a sub-folder of the artwork starts where the artwork is rather than wherever the last dialog was
    /// left.
    /// </para>
    /// <para>
    /// A closed dialog, a dialog that could not be shown and a host with no dialog all leave the box exactly as it
    /// was: choosing nothing is an ordinary answer, and a typed path must not be lost because a mapped drive was
    /// not available. Nothing is stored here — the Save button beside the box still decides that (FR-078).
    /// </para>
    /// </remarks>
    [RelayCommand]
    private Task BrowseHopperFolderAsync() => BrowseIntoAsync(
        HopperFolderInput,
        chosen => HopperFolderInput = chosen,
        ConfigSettingKeys.ImageStorageHopperFolderPathDefault);

    /// <summary>Opens the folder dialog at the lugger picture folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowseLuggerFolderAsync() => BrowseIntoAsync(
        LuggerFolderInput,
        chosen => LuggerFolderInput = chosen,
        ConfigSettingKeys.ImageStorageLuggerFolderPathDefault);

    /// <summary>Opens the folder dialog at the picture folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowsePictureFolderAsync() => BrowseIntoAsync(
        PictureFolderInput,
        chosen => PictureFolderInput = chosen);

    /// <summary>Opens the folder dialog at the dunnage picture root and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowseDunnageRootAsync() => BrowseIntoAsync(
        DunnageRootInput,
        chosen => DunnageRootInput = chosen);

    /// <summary>Opens the folder dialog at the cache folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowsePictureCacheFolderAsync() => BrowseIntoAsync(
        PictureCacheFolderInput,
        chosen => PictureCacheFolderInput = chosen);

    /// <summary>
    /// Asks for a folder, starting where the box points, and hands the answer to the caller.
    /// </summary>
    /// <remarks>
    /// One implementation for every box in the Pictures panel, so all four behave the same way: nothing chosen
    /// leaves the box alone, and nothing is stored — the Save beside the box is what stores a path.
    /// </remarks>
    private async Task BrowseIntoAsync(string? current, Action<string> apply, string? fallback = null)
    {
        if (_folderBrowserService is null)
        {
            return;
        }

        var startingFolder = string.IsNullOrWhiteSpace(current) ? fallback : current.Trim();

        var chosen = await _folderBrowserService
            .PickFolderAsync(startingFolder, CancellationToken.None)
            .ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            apply(chosen);
        }
    }
}
