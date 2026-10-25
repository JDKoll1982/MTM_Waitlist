using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The question add and rename share: what is this lugger called, and which write follows the answer
/// (013-lugger-location-config, FR-003).
/// </summary>
/// <remarks>
/// <para>
/// The answer's write is handed in rather than chosen here, so the window asks one question and the caller says
/// whether the answer adds a lugger or renames one.
/// </para>
/// <para>
/// A refused or unreachable write now reports itself in the shared message window after this one has closed, rather
/// than holding this window open over the failure. The windowed host cannot refuse a close, and the view model keeps
/// the typed name, so nothing is lost: the report names the failure and the editor is one press away.
/// </para>
/// </remarks>
public sealed partial class LuggerNameDialog : UserControl
{
    private readonly Func<Task> _accept;

    public LuggerNameDialog(LuggerArrangementViewModel viewModel, string title, string acceptText, Func<Task> accept)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _accept = accept ?? throw new ArgumentNullException(nameof(accept));

        InitializeComponent();

        HeaderText = title;
        AcceptText = acceptText;

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.PreferredWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    public LuggerArrangementViewModel ViewModel
    {
        get;
    }

    /// <summary>The window's title, kept until the host is built.</summary>
    public string HeaderText { get; }

    /// <summary>The affirmative button's text, kept until the host is built.</summary>
    public string AcceptText { get; }

    /// <summary>
    /// Opens the question in its own window, owned by <paramref name="owner"/>, and runs the answer's write when the
    /// person accepts.
    /// </summary>
    /// <param name="owner">The application window the question is centred on and belongs to.</param>
    public async Task ShowWindowedAsync(Window owner)
    {
        var host = WindowedDialogHost.Create(
            owner,
            HeaderText,
            this,
            AcceptText,
            ViewModel.CancelActionText,
            MessageDialogButton.Act);

        var result = await host.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        while (true)
        {
            await _accept();

            if (string.IsNullOrWhiteSpace(ViewModel.MessageText))
            {
                return;
            }

            var retry = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
            {
                Kind = MessageKind.Retry,
                Title = HeaderText,
                Message = ViewModel.MessageText,
                ActLabel = AcceptText,
                CancelLabel = "Close",
            });

            if (!retry.IsAct)
            {
                return;
            }
        }
    }
}
