using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// One lugger's editor, opened from that lugger's own card (013-lugger-location-config, FR-019, FR-030, FR-031).
/// </summary>
/// <remarks>
/// <para>
/// The window holds no rule of its own: every action reaches the page's view model, which is where the write, the
/// store's refusal and the unavailable state are decided. It carries no lugger picker, because the card that
/// opened it already named the lugger.
/// </para>
/// <para>
/// It is the <b>content</b> of a windowed dialog rather than a dialog drawn inside the settings page. A
/// <c>WindowedContentDialog</c> opens its own window owned by the main one, so it is centred on the application
/// window and stays centred while that window is resized — something a dialog hosted inside a page cannot do once
/// the page itself is a scrolling surface.
/// </para>
/// </remarks>
public sealed partial class LuggerEditorDialog : UserControl
{
    public LuggerEditorDialog(LuggerArrangementViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.PreferredWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    public LuggerArrangementViewModel ViewModel
    {
        get;
    }

    /// <summary>
    /// Opens the editor in its own window, owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="header">The window's title: the lugger this editor is about.</param>
    /// <param name="owner">The application window the editor is centred on and belongs to.</param>
    /// <returns>The button the window was closed with.</returns>
    public async Task<ContentDialogResult> ShowWindowedAsync(string header, Window owner)
    {
        var dialog = WindowedDialogHost.Create(
            owner,
            header,
            this,
            actLabel: null,
            cancelLabel: ViewModel.CloseEditorActionText,
            MessageDialogButton.Cancel);

        return await dialog.ShowAsync();
    }
}
