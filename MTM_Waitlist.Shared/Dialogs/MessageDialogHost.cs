using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The one entry point a screen uses when it needs an answer (014-adopt-lab-design-system, FR-008, FR-012).
/// </summary>
/// <remarks>
/// <para>
/// A screen describes the question and reads the answer. It does not build a dialog, does not choose a button order,
/// and cannot make a destructive answer the default — those belong to <see cref="MessageKind"/> and to this class.
/// </para>
/// <para>
/// The call is UI-thread bound and returns only when the dialog window has closed, so a caller that ignores the answer
/// still waits for the person to finish reading.
/// </para>
/// </remarks>
public static class MessageDialogHost
{
    /// <summary>
    /// Asks <paramref name="request"/> in a window owned by and centred on <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The window the dialog belongs to. Never the desktop, and never omitted.</param>
    /// <param name="request">The question to ask.</param>
    /// <returns>The answer taken.</returns>
    /// <exception cref="ArgumentException">The request is blank or is missing a label its kind requires.</exception>
    public static async Task<MessageDialogOutcome> AskAsync(Window owner, MessageDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var spec = MessageKindSpec.For(request.Kind);
        var labels = spec.ResolveLabels(request);

        var content = new MessageDialogContent();
        content.Load(request);

        ContentDialogResult result = await WindowedDialogHost.ShowAsync(
            owner,
            request.Title,
            content,
            labels.ActLabel,
            labels.CancelLabel,
            spec.DefaultButton);

        var button = result == ContentDialogResult.Primary ? MessageDialogButton.Act : MessageDialogButton.Cancel;

        return new MessageDialogOutcome(
            MessageKindSpec.OutcomeFor(request.Kind, button),
            MessageKindSpec.IsDefault(request.Kind, button));
    }

    private static void Validate(MessageDialogRequest request)
    {
        if (!Enum.IsDefined(request.Kind))
        {
            throw new ArgumentException("The message kind is not one of the six standard questions.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("A message dialog needs a title.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("A message dialog needs a message.", nameof(request));
        }

        var needsCallerLabel = request.Kind
            is MessageKind.Confirm
            or MessageKind.DestructiveConfirm
            or MessageKind.Retry;

        if (needsCallerLabel && string.IsNullOrWhiteSpace(request.ActLabel))
        {
            throw new ArgumentException(
                "This message kind's affirmative button needs a label, because only the screen knows what the action is.",
                nameof(request));
        }
    }
}
