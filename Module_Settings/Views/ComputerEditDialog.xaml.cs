using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The body of the computer editor, shown in its own window owned by the main one
/// (014-adopt-lab-design-system, FR-001).
/// </summary>
public sealed partial class ComputerEditDialog : UserControl
{
    private readonly ComputerEditDialogViewModel _viewModel;

    public ComputerEditDialog(ComputerEditDialogViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.PreferredWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    public ComputerEditDialogViewModel ViewModel => _viewModel;

    /// <summary>
    /// Opens the editor in its own window, owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The application window the editor is centred on and belongs to.</param>
    /// <returns><see langword="true"/> when the person saved.</returns>
    public async Task<bool> ShowWindowedAsync(Window owner)
    {
        var host = WindowedDialogHost.Create(
            owner,
            ViewModel.Title,
            this,
            "Save",
            "Cancel",
            MessageDialogButton.Act);

        var result = await host.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    private void Field_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.ValidateCommand.Execute(null);
    }
}
