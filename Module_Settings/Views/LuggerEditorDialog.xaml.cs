using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// One lugger's editor, opened from that lugger's own card (013-lugger-location-config, FR-019, FR-030, FR-031).
/// </summary>
/// <remarks>
/// The window holds no rule of its own: every action reaches the page's view model, which is where the write, the
/// store's refusal and the unavailable state are decided. It carries no lugger picker, because the card that
/// opened it already named the lugger.
/// </remarks>
public sealed partial class LuggerEditorDialog : ContentDialog
{
    public LuggerEditorDialog(LuggerArrangementViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public LuggerArrangementViewModel ViewModel
    {
        get;
    }
}
