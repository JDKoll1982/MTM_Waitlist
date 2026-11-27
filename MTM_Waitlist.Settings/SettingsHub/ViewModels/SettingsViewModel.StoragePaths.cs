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
    /// Loads the two storage folders and the retention period, then reports any disagreement with this machine's
    /// own settings file.
    /// </summary>
    /// <remarks>
    /// Started from the constructor and awaited by nothing, like the other panels here: the page must not block on
    /// a store read. A read that fails leaves the boxes empty and says so, rather than showing a value nobody
    /// configured.
    /// </remarks>
    private async Task InitializeStoragePathsAsync()
    {
        if (_imageStorageConfigurationResolver is null)
        {
            return;
        }

        try
        {
            PictureFolderPath = await _imageStorageConfigurationResolver
                .GetSharedFolderPathAsync()
                .ConfigureAwait(true);
            PictureFolderInput = PictureFolderPath;

            DunnageRootInput = await _imageStorageConfigurationResolver
                .GetDunnageRootPathAsync()
                .ConfigureAwait(true);

            HopperFolderInput = await _imageStorageConfigurationResolver
                .GetHopperFolderPathAsync()
                .ConfigureAwait(true);

            LuggerFolderInput = await _imageStorageConfigurationResolver
                .GetLuggerFolderPathAsync()
                .ConfigureAwait(true);

            NoImagePathInput = await _imageStorageConfigurationResolver
                .GetNoImagePathAsync()
                .ConfigureAwait(true);

            // Adopted here as well as at startup: a host whose launch steps did not run the picture cache still gets
            // the plant's artwork as soon as anyone opens this screen, rather than the shipped one for the rest of
            // the run.
            ImagePicturePolicy.SetNoImageOverride(NoImagePathInput);

            ArchiveKeepDaysInput = await _imageStorageConfigurationResolver
                .GetArchiveKeepDaysAsync()
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_Unavailable.Text".GetLocalized();
            AppLog.Error(
                "SettingsStoragePaths",
                ex,
                "The storage folders could not be read; the boxes are left empty and the store is asked again next visit.");
        }
        finally
        {
            RefreshSearchVisibility();
        }
    }

    /// <summary>
    /// Stores the picture folder, the dunnage root and the retention period for every computer.
    /// </summary>
    /// <remarks>
    /// One button for the three, because they are one subject and one screen: three buttons over three boxes on one
    /// panel is three chances to leave the panel half saved. The period is refused when it is not a positive whole
    /// number of days rather than stored as a figure the cleanup would read as zero.
    /// </remarks>
    [RelayCommand]
    private async Task SaveStoragePathsAsync()
    {
        if (_configSettingsValueService is null || _imageStorageConfigurationResolver is null || IsStoragePathsBusy)
        {
            return;
        }

        var pictureFolder = PictureFolderInput?.Trim() ?? string.Empty;
        var dunnageRoot = DunnageRootInput?.Trim() ?? string.Empty;
        var hopperFolder = HopperFolderInput?.Trim() ?? string.Empty;
        var luggerFolder = LuggerFolderInput?.Trim() ?? string.Empty;
        var noImagePath = NoImagePathInput?.Trim() ?? string.Empty;
        var keepDays = (int)Math.Round(ArchiveKeepDaysInput, MidpointRounding.AwayFromZero);

        if (pictureFolder.Length == 0 || dunnageRoot.Length == 0)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_FolderNeeded.Text".GetLocalized();
            return;
        }

        if (hopperFolder.Length == 0)
        {
            // An empty hopper folder is refused rather than stored: it would leave the three sizes pointing at the
            // picture root itself, and the card would then never find a hopper picture at all (FR-078).
            StoragePathsStatusMessage = "Settings_StoragePaths_HopperFolderNeeded.Text".GetLocalized();
            return;
        }

        if (keepDays < 1)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_DaysNeeded.Text".GetLocalized();
            return;
        }

        if (luggerFolder.Length == 0)
        {
            // Refused for the hopper folder's reason: an empty value is not a folder, and storing it would leave the
            // lugger cards reading a path that resolves to nothing at all.
            StoragePathsStatusMessage = "Settings_StoragePaths_LuggerFolderNeeded.Text".GetLocalized();
            return;
        }

        IsStoragePathsBusy = true;

        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageSharedFolderPath,
                "text",
                pictureFolder,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.DunnageRootPath,
                "text",
                dunnageRoot,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageHopperFolderPath,
                "text",
                hopperFolder,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageLuggerFolderPath,
                "text",
                luggerFolder,
                boolean: null).ConfigureAwait(true);

            // Empty is a legitimate answer — it means the artwork that ships with the application — so it is stored
            // as the shipped path rather than as a blank the resolver would have to interpret.
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageNoImagePath,
                "text",
                noImagePath.Length == 0 ? ConfigSettingKeys.ImageStorageNoImagePathDefault : noImagePath,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageArchiveKeepDays,
                "int",
                text: null,
                boolean: null,
                integer: keepDays).ConfigureAwait(true);

            PictureFolderPath = pictureFolder;
            NoImagePathInput = noImagePath.Length == 0
                ? ConfigSettingKeys.ImageStorageNoImagePathDefault
                : noImagePath;

            // Adopted at once rather than at the next start, so the change the person just made is the change they
            // see when they walk to the next screen.
            ImagePicturePolicy.SetNoImageOverride(NoImagePathInput);

            StoragePathsStatusMessage = noImagePath.Length == 0
                || ImagePicturePolicy.ResolveExistingFile(noImagePath) is not null
                ? "Settings_StoragePaths_Saved.Text".GetLocalized()
                : "Settings_StoragePaths_NoImageMissing.Text".GetLocalized();
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_SaveFailed.Text".GetLocalized();
            AppLog.Error("SettingsStoragePaths", ex, "A storage setting could not be stored.");
        }
        finally
        {
            IsStoragePathsBusy = false;
        }
    }
}
