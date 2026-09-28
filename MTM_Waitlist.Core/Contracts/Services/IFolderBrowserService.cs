namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Asks the person to choose a folder, and answers the one they chose.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is a seam, not a call.</b> The folder dialog belongs to the surface that shows it, and a view model that
/// opened one directly could not be driven by a test at all. Everything that needs a folder asks this, and the
/// application supplies the implementation that knows which window is on screen.
/// </para>
/// <para>
/// <b>A refused or abandoned choice is <c>null</c>, never an exception.</b> Closing the dialog is an ordinary
/// answer meaning "leave what I typed alone", so it is reported the same way as choosing nothing, and a folder
/// dialog that could not be shown does not become a failure of the screen that offered it.
/// </para>
/// </remarks>
public interface IFolderBrowserService
{
    /// <summary>
    /// Shows the folder dialog and answers the folder the person chose.
    /// </summary>
    /// <param name="startingFolder">
    /// Where to open the dialog, which is what the field already holds or the suggested default. A path that does
    /// not resolve is not an error: the dialog opens wherever it can.
    /// </param>
    /// <param name="cancellationToken">Cancels the wait for the person's choice.</param>
    /// <returns>The chosen folder's full path, or <c>null</c> when nothing was chosen.</returns>
    Task<string?> PickFolderAsync(string? startingFolder, CancellationToken cancellationToken);
}
