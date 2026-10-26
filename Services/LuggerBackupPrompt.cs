using System.Globalization;

using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="ILuggerBackupPrompt"/>, owning the window the question is asked in.
/// </summary>
/// <remarks>
/// With no owner window (a headless host) the answer is "no", so an unanswered question can never be read as
/// consent to change a second lugger's status.
/// </remarks>
public sealed class LuggerBackupPrompt : ILuggerBackupPrompt
{
    /// <inheritdoc />
    public async Task<bool> AskToMakeBackupAvailableAsync(
        string changedLuggerName,
        string scrapType,
        string backupLuggerName,
        CancellationToken cancellationToken = default)
    {
        if (App.MainWindow is not { } owner)
        {
            return false;
        }

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Confirm,
            Title = "Settings_Lugger_Arrangement.BackupPromptTitle".GetLocalized(),
            Message = string.Format(
                CultureInfo.CurrentCulture,
                "Settings_Lugger_Arrangement.BackupPromptMessage".GetLocalized(),
                changedLuggerName,
                scrapType,
                backupLuggerName),
            ActLabel = "Settings_Lugger_Arrangement.BackupPromptAct".GetLocalized(),
            CancelLabel = "Settings_Lugger_Arrangement.BackupPromptKeep".GetLocalized(),
        });

        return outcome.IsAct;
    }
}
