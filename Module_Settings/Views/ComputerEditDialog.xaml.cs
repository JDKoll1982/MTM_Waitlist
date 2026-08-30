using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class ComputerEditDialog : ContentDialog
{
    private readonly ComputerEditDialogViewModel _viewModel;

    public ComputerEditDialog(ComputerEditDialogViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public ComputerEditDialogViewModel ViewModel => _viewModel;

    private void Field_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.ValidateCommand.Execute(null);
    }
}
