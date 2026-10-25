namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// What the person answered.
/// </summary>
public enum MessageOutcome
{
    /// <summary>The message was read. The only answer an acknowledgement offers.</summary>
    Acknowledged,

    /// <summary>The affirmative button was taken.</summary>
    Act,

    /// <summary>The dismissive button, Escape, or the window's own close button was taken.</summary>
    Cancel,
}

/// <summary>
/// The answer to a <see cref="MessageDialogRequest"/>, in one shape for every kind.
/// </summary>
/// <param name="Chosen">Which answer was taken.</param>
/// <param name="WasDefault">Whether the answer taken was the kind's default, so a screen can tell a deliberate choice
/// from a person who simply pressed Enter.</param>
public readonly record struct MessageDialogOutcome(MessageOutcome Chosen, bool WasDefault)
{
    /// <summary>Whether the affirmative answer was taken.</summary>
    public bool IsAct => Chosen == MessageOutcome.Act;

    /// <summary>Whether the dismissive answer was taken.</summary>
    public bool IsCancel => Chosen == MessageOutcome.Cancel;
}
