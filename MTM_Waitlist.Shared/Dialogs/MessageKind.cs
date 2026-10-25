namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The six standard questions the application asks (014-adopt-lab-design-system, FR-008).
/// </summary>
/// <remarks>
/// A closed set. A screen that needs to ask something the set does not cover adds a member here rather than opening an
/// ad-hoc dialog of its own, so the whole application keeps asking permission the same way.
/// </remarks>
public enum MessageKind
{
    /// <summary>One acknowledgement button. Used when there is nothing to decide, only something to read.</summary>
    Acknowledge,

    /// <summary>An act and a cancel, with the act as the default.</summary>
    Confirm,

    /// <summary>A yes and a no, with yes as the default.</summary>
    Decide,

    /// <summary>An act that destroys something, with cancel as the default so a stray Enter keeps the data.</summary>
    DestructiveConfirm,

    /// <summary>Leaving a screen that holds unsaved work, with staying as the default.</summary>
    LeaveWithoutSaving,

    /// <summary>An operation that failed and can be attempted again, with the retry as the default.</summary>
    Retry,
}

/// <summary>
/// The two button slots a message dialog can fill.
/// </summary>
public enum MessageDialogButton
{
    /// <summary>The affirmative slot, bound to the window's primary button.</summary>
    Act,

    /// <summary>The dismissive slot, bound to the window's close button. Also where a lone acknowledgement lives.</summary>
    Cancel,
}
