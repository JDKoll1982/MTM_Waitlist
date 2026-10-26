namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The question the arrangement asks when a lugger stops being <c>Available</c> and another lugger on the same
/// scrap type is only a <c>Backup</c> (owner decision, 2026-10-26).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is a question, not a rule.</b> The store does not care whether a scrap type still has an available lugger
/// to walk to, so nothing here refuses the status change: the person is told what the change costs them and offered
/// the one action that removes the cost. Answering no keeps the arrangement exactly as it is and leaves the
/// responsibility with them, which is stated in the editor's own message rather than in a dialog that disappears.
/// </para>
/// <para>
/// It lives in Settings, beside the view model that asks it, and is implemented in the application project — the
/// composition root — because showing a dialog needs a window. A headless host (the unit suite) supplies an answer
/// of its own, so the view model stays constructible with no UI at all.
/// </para>
/// </remarks>
public interface ILuggerBackupPrompt
{
    /// <summary>
    /// Asks whether a backup lugger should be made available for the scrap type the changed lugger was holding.
    /// </summary>
    /// <param name="changedLuggerName">The lugger whose status is being changed away from <c>Available</c>.</param>
    /// <param name="scrapType">The scrap type whose only remaining lugger is a backup, as it is spelled.</param>
    /// <param name="backupLuggerName">The backup lugger that could be made available.</param>
    /// <param name="cancellationToken">Cancels the wait, which answers no.</param>
    /// <returns><see langword="true"/> only when the person chose to make the backup available.</returns>
    Task<bool> AskToMakeBackupAvailableAsync(
        string changedLuggerName,
        string scrapType,
        string backupLuggerName,
        CancellationToken cancellationToken = default);
}
