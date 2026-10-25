using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The question add and rename share: what is this lugger called, and which write follows the answer
/// (013-lugger-location-config, FR-003).
/// </summary>
/// <remarks>
/// The answer's write is handed in rather than chosen here, so the window asks one question and the caller says
/// whether the answer adds a lugger or renames one. A write that was refused, or that could not reach the store,
/// leaves its sentence behind and the window stays open over it rather than closing on a failure.
/// </remarks>
public sealed partial class LuggerNameDialog : ContentDialog
{
    private readonly Func<Task> _accept;

    public LuggerNameDialog(LuggerArrangementViewModel viewModel, string title, string acceptText, Func<Task> accept)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _accept = accept ?? throw new ArgumentNullException(nameof(accept));

        InitializeComponent();

        Title = title;
        PrimaryButtonText = acceptText;
    }

    public LuggerArrangementViewModel ViewModel
    {
        get;
    }

    private async void OnAccepted(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferred = args.GetDeferral();

        try
        {
            await _accept();

            args.Cancel = !string.IsNullOrWhiteSpace(ViewModel.MessageText);
        }
        finally
        {
            deferred.Complete();
        }
    }
}
