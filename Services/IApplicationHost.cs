using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Services;

/// <summary>
/// What the one window and the launch surfaces need from the host: the two routes out of the process, the two
/// pieces of state the window's close box is answered with, and the window chrome the theme hook repaints.
/// </summary>
/// <remarks>
/// <para>
/// It is an interface rather than the host type itself so the launch surfaces and the window that shows them name
/// no reference back to the composition root. The host creates every one of them, so a surface holding a reference
/// to the host made the two depend on each other; each now takes what it needs from this seam, and the behaviour is
/// the host's own, unchanged.
/// </para>
/// <para>
/// Every member forwards to a member the host already had, so nothing about the launch, the endings or the window
/// changes: the same window is closed the same way, the same reason is stated, and the same shutdown runs.
/// </para>
/// </remarks>
internal interface IApplicationHost
{
    /// <summary>The title bar's own content, which the theme hook repaints beside the caption buttons.</summary>
    UIElement? Titlebar { get; }

    /// <summary>
    /// Whether the launch has handed over to the shell, after which closing the window is an ordinary close rather
    /// than a route out of a launch stage (FR-008, FR-022).
    /// </summary>
    bool HasHandedOverToShell { get; }

    /// <summary>
    /// Whether the process has already been asked to end, so a close that is part of the ending is not mistaken for
    /// a route out of a launch stage.
    /// </summary>
    bool IsExitRequested { get; }

    /// <summary>
    /// Answers the one window's close box while a launch stage is showing, by asking the stage what closing it
    /// means (FR-008).
    /// </summary>
    void OnWindowClosingDuringLaunch();

    /// <summary>Stops the host, so no background service is still writing when the process goes away.</summary>
    /// <returns>A task that completes when the host has stopped or its bounded wait has run out.</returns>
    Task ShutdownAsync();

    /// <summary>Ends the process once the reason has been stated, exactly once (FR-008).</summary>
    void ExitApplication();
}
