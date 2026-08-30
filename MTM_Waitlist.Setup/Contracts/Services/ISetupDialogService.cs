using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Contracts.Services;

/// <summary>
/// Provides app-owned dialog interactions for the Setup dunnage workflow.
/// Implemented by the composition root (app) because showing the dialogs requires
/// the app-side <c>SetupDunnageImageSearchDialog</c> view and a live XAML root.
/// View models depend on this abstraction so they never reference the app static
/// or app-owned views.
/// </summary>
public interface ISetupDialogService
{
    /// <summary>
    /// Shows the image-backed dunnage part search dialog and returns the part the
    /// user picked (or <c>null</c> when dismissed or no XAML root is available).
    /// </summary>
    Task<SetupDunnagePart?> ShowDunnageImageSearchDialogAsync();

    /// <summary>
    /// Asks the user whether to continue without selecting dunnage.
    /// Returns <c>true</c> to continue, <c>false</c> to cancel. Returns <c>true</c>
    /// when no XAML root is available (for example a headless test host).
    /// </summary>
    Task<bool> ConfirmNoDunnageAsync();
}
