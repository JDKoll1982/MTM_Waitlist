using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// What a surface in this service needs from the running application: the container its view model comes from, and
/// the one window a dialog has to be owned by.
/// </summary>
/// <remarks>
/// <para>
/// It is an interface rather than the application type itself so a page names no reference back to the application
/// that created it. The application builds every surface, so a page holding a reference to it made the two depend
/// on each other; a page now reads this seam instead, and the values behind it are the application's own.
/// </para>
/// <para>
/// Both members state the absence they can have rather than throwing, because both absences are real: the service
/// is tray-only until something asks for a window, so there is no window to own a dialog until then, and the
/// container does not exist until the engines are up.
/// </para>
/// </remarks>
internal interface IServiceRuntime
{
    /// <summary>The service's container, or <c>null</c> before it has been built.</summary>
    IServiceProvider? Services { get; }

    /// <summary>
    /// The service window, when it exists, for anything that has to own a dialog — or <c>null</c> before one has
    /// been asked for.
    /// </summary>
    /// <remarks>
    /// A caller that needs an owner and finds none must not open a dialog at all: an unowned dialog has no window
    /// to be centred on and cannot be dismissed with the window it was raised from.
    /// </remarks>
    Window? ShellWindow { get; }
}
