using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The one place the application opens a windowed dialog (014-adopt-lab-design-system, FR-001, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// Every modal surface goes through here rather than building a dialog of its own, because the three things that make a
/// windowed dialog behave are easy to leave out and invisible until a person hits them: the owner must be set or the
/// window opens behind the application, the show call must be awaited directly or the modal lifetime breaks, and the
/// content must arrive with a size or the window opens at whatever the platform guesses.
/// </para>
/// <para>
/// This is a host, not a dialog. It carries no message, no button order of its own and no default button; those come
/// from <see cref="MessageDialogHost"/> and from the screen.
/// </para>
/// </remarks>
public static class WindowedDialogHost
{
    /// <summary>
    /// Builds a dialog window owned by and centred on <paramref name="owner"/>, without showing it.
    /// </summary>
    /// <remarks>
    /// For a surface that has to answer its own buttons — a search dialog that fills a result, an editor that refuses
    /// to close on a failed save — the caller needs the window before it opens so it can attach to those events, then
    /// awaits <see cref="ContentDialog.ShowAsync"/> itself.
    /// </remarks>
    /// <param name="owner">The window the dialog belongs to. Never the desktop, and never omitted.</param>
    /// <param name="title">The dialog window's title.</param>
    /// <param name="content">What the dialog shows. Sizes itself where it needs to.</param>
    /// <param name="actLabel">The affirmative button's text, or <see langword="null"/> for no affirmative button.</param>
    /// <param name="cancelLabel">The dismissive button's text. Always supplied, because a window needs a way out.</param>
    /// <param name="defaultButton">Which button Enter takes.</param>
    public static DevWinUI.WindowedContentDialog Create(
        Window owner,
        string title,
        object content,
        string? actLabel,
        string cancelLabel,
        MessageDialogButton defaultButton)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A dialog needs a title.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(cancelLabel))
        {
            throw new ArgumentException("A dialog needs a way out, so a dismissive label is required.", nameof(cancelLabel));
        }

        var dialog = new DevWinUI.WindowedContentDialog
        {
            Header = title.Trim(),
            Content = content,
            CloseButtonContent = cancelLabel.Trim(),
            DefaultButton = defaultButton == MessageDialogButton.Act
                ? ContentDialogButton.Primary
                : ContentDialogButton.Close,
            Owner = owner,
        };

        if (!string.IsNullOrWhiteSpace(actLabel))
        {
            dialog.PrimaryButtonContent = actLabel.Trim();
        }

        return dialog;
    }

    /// <summary>
    /// Presents <paramref name="content"/> in its own window, owned by and centred on <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The window the dialog belongs to. Never the desktop, and never omitted.</param>
    /// <param name="title">The dialog window's title.</param>
    /// <param name="content">What the dialog shows. Sizes itself where it needs to.</param>
    /// <param name="actLabel">The affirmative button's text, or <see langword="null"/> for no affirmative button.</param>
    /// <param name="cancelLabel">The dismissive button's text. Always supplied, because a window needs a way out.</param>
    /// <param name="defaultButton">Which button Enter takes.</param>
    /// <returns>The button the window was closed with.</returns>
    public static async Task<ContentDialogResult> ShowAsync(
        Window owner,
        string title,
        object content,
        string? actLabel,
        string cancelLabel,
        MessageDialogButton defaultButton)
    {
        var dialog = Create(owner, title, content, actLabel, cancelLabel, defaultButton);

        // Awaited directly. Deferring the continuation is forbidden by the api rules and breaks the modal lifetime.
        return await dialog.ShowAsync();
    }
}
