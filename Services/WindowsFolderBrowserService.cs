using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

using Windows.Storage.Pickers;

namespace MTM_Waitlist.Services;

/// <summary>
/// The folder dialog the surfaces actually show.
/// </summary>
/// <remarks>
/// <para>
/// <b>It must be owned by a window.</b> A folder dialog raised without one is refused outright on this
/// application's build, because it is an unpackaged desktop process: there is no package identity for the picker
/// to fall back on and no ambient window for it to parent itself to. The window the person is looking at is
/// therefore handed to it explicitly.
/// </para>
/// <para>
/// <b>A dialog that cannot be shown answers null rather than throwing.</b> Choosing a folder is a convenience
/// beside a text box a person can type into, so a picker that fails must not take the screen down with it.
/// </para>
/// </remarks>
internal sealed class WindowsFolderBrowserService : IFolderBrowserService
{
    /// <summary>The module name a failure here is recorded under.</summary>
    private const string LogModule = "StartupLaunch";

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string? startingFolder, CancellationToken cancellationToken)
    {
        try
        {
            var picker = new FolderPicker();

            // The dialog filters by extension, and a folder picker refuses to open with no filter at all.
            picker.FileTypeFilter.Add("*");

            if (App.CurrentSurfaceWindow is not { } window)
            {
                // Nothing on screen to own the dialog, so it is not shown rather than shown unparented.
                AppLog.Info(LogModule, "No window was on screen to own the folder dialog, so none was shown.");

                return null;
            }

            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(window));

            if (!string.IsNullOrWhiteSpace(startingFolder) && Directory.Exists(startingFolder))
            {
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.CommitButtonText = "Use this folder";
            }

            var chosen = await picker.PickSingleFolderAsync();

            return string.IsNullOrWhiteSpace(chosen?.Path) ? null : chosen.Path;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogModule, exception, "The folder dialog could not be shown, so nothing was chosen.");

            return null;
        }
    }
}
