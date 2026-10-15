using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// Shows one control's details to the person who asked for them, in the inspector's own window.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is not a page.</b> The inspector is opened from a control that is hovered, and a hovered control is
/// very often inside a modal dialog — a <c>ContentDialog</c> owns its <c>XamlRoot</c> and blocks it, so a page
/// navigated to behind that dialog can never be reached or even seen, and the shortcut appears to do nothing. A
/// window has no <c>XamlRoot</c> of its own to be blocked by, so it opens over the dialog that raised it, over the
/// shell, or over any other window the app shows.
/// </para>
/// <para>
/// The seam exists because the service lives in a library that may not reference the windows: it asks for the
/// details to be presented and is told whether they were, and the implementation that opens the window lives with
/// the views.
/// </para>
/// </remarks>
public interface IControlInspectorDetailPresenter
{
    /// <summary>
    /// Presents <paramref name="detail"/>, reusing the inspector's window when it is already open. Returns
    /// <see langword="false" /> when nothing could be shown.
    /// </summary>
    bool Present(ControlInspectorDetail detail);
}
