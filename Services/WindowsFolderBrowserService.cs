using Microsoft.Windows.Storage.Pickers;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Services;

/// <summary>
/// The folder dialog the surfaces actually show.
/// </summary>
/// <remarks>
/// <para>
/// <b>It must be owned by a window.</b> A folder dialog raised without one is refused outright on this
/// application's build, because it is an unpackaged desktop process: there is no package identity for the picker
/// to fall back on and no ambient window for it to parent itself to. The Windows App SDK picker is therefore built
/// with the window identifier of the window the person is looking at.
/// </para>
/// <para>
/// <b>It opens where it was asked to.</b> The Windows App SDK picker takes the folder to open at as a path
/// (<see cref="FolderPicker.SuggestedFolder"/>), so the dialog starts on the share the box is already pointing at.
/// The <c>Windows.Storage</c> picker this replaced could only be given one of the well-known locations, which put a
/// mapped network drive out of reach.
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
            if (App.CurrentSurfaceWindow is not { } window)
            {
                // Nothing on screen to own the dialog, so it is not shown rather than shown unparented.
                AppLog.Info(LogModule, "No window was on screen to own the folder dialog, so none was shown.");

                return null;
            }

            var picker = new FolderPicker(window.AppWindow.Id);

            // A folder that is not there is not an error: the dialog then opens wherever it can, which is what
            // keeps an unreachable share from turning into a failure of the screen that offered the box.
            if (!string.IsNullOrWhiteSpace(startingFolder) && Directory.Exists(startingFolder))
            {
                picker.SuggestedFolder = startingFolder;
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
