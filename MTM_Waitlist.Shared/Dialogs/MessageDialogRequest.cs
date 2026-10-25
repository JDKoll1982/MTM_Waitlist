namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// What a screen asks for when it needs a person's answer (014-adopt-lab-design-system, FR-009).
/// </summary>
/// <remarks>
/// The screen supplies the words and nothing else. It cannot choose the button order, the default button or the answer
/// that Escape gives, because those are fixed by <see cref="Kind"/> — a dialog that let its caller pick the default
/// would let one screen make a destructive answer the easy one.
/// </remarks>
public sealed record MessageDialogRequest
{
    /// <summary>Which of the six questions this is.</summary>
    public required MessageKind Kind { get; init; }

    /// <summary>
    /// The window's title. Names the subject of the question rather than only the action, so the button below it is not
    /// the only thing naming what is about to happen.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>What is about to happen, and what it costs. Names the consequence of the default answer.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// The verb for the affirmative button, such as <c>Remove</c>. Required for the kinds whose affirmative is the
    /// screen's own action, because only the screen knows what that action is.
    /// </summary>
    public string? ActLabel { get; init; }

    /// <summary>The dismissive label, when the kind's own word for it is not the one the screen means.</summary>
    public string? CancelLabel { get; init; }

    /// <summary>The named thing the question is about, echoed under the message so it cannot be confused with another.</summary>
    public string? Subject { get; init; }
}
