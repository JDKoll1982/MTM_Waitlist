namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The fixed behaviour of one <see cref="MessageKind"/>: which buttons it shows, which one is the default, and what each
/// button means (014-adopt-lab-design-system, FR-008, FR-010).
/// </summary>
/// <remarks>
/// Pure, and deliberately so. The button order, the default and the meaning of Escape are the things a screen must not
/// be able to get wrong, so they live here as data and are asserted by the test suite rather than being rebuilt inside
/// each dialog.
/// </remarks>
public sealed class MessageKindSpec
{
    private static readonly MessageKindSpec AcknowledgeSpec = new(MessageKind.Acknowledge, null, "OK", MessageDialogButton.Cancel, MessageOutcome.Acknowledged, MessageOutcome.Acknowledged);

    private static readonly MessageKindSpec ConfirmSpec = new(MessageKind.Confirm, null, "Cancel", MessageDialogButton.Act, MessageOutcome.Act, MessageOutcome.Cancel);

    private static readonly MessageKindSpec DecideSpec = new(MessageKind.Decide, "Yes", "No", MessageDialogButton.Act, MessageOutcome.Act, MessageOutcome.Cancel);

    private static readonly MessageKindSpec DestructiveConfirmSpec = new(MessageKind.DestructiveConfirm, null, "Cancel", MessageDialogButton.Cancel, MessageOutcome.Act, MessageOutcome.Cancel);

    private static readonly MessageKindSpec LeaveWithoutSavingSpec = new(MessageKind.LeaveWithoutSaving, "Leave", "Stay", MessageDialogButton.Cancel, MessageOutcome.Act, MessageOutcome.Cancel);

    private static readonly MessageKindSpec RetrySpec = new(MessageKind.Retry, "Try again", "Cancel", MessageDialogButton.Act, MessageOutcome.Act, MessageOutcome.Cancel);

    private MessageKindSpec(
        MessageKind kind,
        string? actLabel,
        string cancelLabel,
        MessageDialogButton defaultButton,
        MessageOutcome actOutcome,
        MessageOutcome cancelOutcome)
    {
        Kind = kind;
        ActLabel = actLabel;
        CancelLabel = cancelLabel;
        DefaultButton = defaultButton;
        ActOutcome = actOutcome;
        CancelOutcome = cancelOutcome;
    }

    /// <summary>The kind this describes.</summary>
    public MessageKind Kind { get; }

    /// <summary>
    /// The kind's own word for the affirmative button, or <see langword="null"/> when the screen has to supply one
    /// because only it knows what the action is.
    /// </summary>
    public string? ActLabel { get; }

    /// <summary>The kind's own word for the dismissive button.</summary>
    public string CancelLabel { get; }

    /// <summary>Which button is the default, and therefore which one Enter takes.</summary>
    public MessageDialogButton DefaultButton { get; }

    /// <summary>What the affirmative button means.</summary>
    public MessageOutcome ActOutcome { get; }

    /// <summary>What the dismissive button, Escape and the window's close button mean.</summary>
    public MessageOutcome CancelOutcome { get; }

    /// <summary>Whether the kind shows an affirmative button at all.</summary>
    public bool HasAct => Kind != MessageKind.Acknowledge;

    /// <summary>The fixed behaviour of <paramref name="kind"/>.</summary>
    public static MessageKindSpec For(MessageKind kind) => kind switch
    {
        MessageKind.Acknowledge => AcknowledgeSpec,
        MessageKind.Confirm => ConfirmSpec,
        MessageKind.Decide => DecideSpec,
        MessageKind.DestructiveConfirm => DestructiveConfirmSpec,
        MessageKind.LeaveWithoutSaving => LeaveWithoutSavingSpec,
        MessageKind.Retry => RetrySpec,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not one of the six standard questions."),
    };

    /// <summary>What <paramref name="button"/> means for <paramref name="kind"/>.</summary>
    public static MessageOutcome OutcomeFor(MessageKind kind, MessageDialogButton button)
    {
        var spec = For(kind);
        return button == MessageDialogButton.Act ? spec.ActOutcome : spec.CancelOutcome;
    }

    /// <summary>Whether <paramref name="button"/> is the one Enter takes for <paramref name="kind"/>.</summary>
    public static bool IsDefault(MessageKind kind, MessageDialogButton button) => For(kind).DefaultButton == button;

    /// <summary>
    /// Resolves the labels a request will actually show, filling the kind's own words where the caller left a label out.
    /// </summary>
    /// <remarks>
    /// The kind fixes the order, the position, the default and what each answer means; the caller may still name its own
    /// action, because "Remove" and "Clear all" are the same slot with different words and a screen that lost its wording
    /// would be a change to the flow rather than to the dialog (FR-009, FR-027).
    /// </remarks>
    /// <returns>The affirmative label, or <see langword="null"/> for a kind with one button; and the dismissive label.</returns>
    public (string? ActLabel, string CancelLabel) ResolveLabels(MessageDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var act = Kind == MessageKind.Acknowledge
            ? null
            : string.IsNullOrWhiteSpace(request.ActLabel) ? ActLabel : request.ActLabel!.Trim();

        return (act, string.IsNullOrWhiteSpace(request.CancelLabel) ? CancelLabel : request.CancelLabel!.Trim());
    }
}
