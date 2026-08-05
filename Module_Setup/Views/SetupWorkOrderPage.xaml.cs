using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkOrderPage : Page
{
    public SetupWorkOrderViewModel ViewModel { get; }

    public SetupWorkOrderPage()
    {
        ViewModel = App.GetService<SetupWorkOrderViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _ = WorkOrderTextBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupWorkOrderViewModel.StatusMessage)
            && !string.IsNullOrWhiteSpace(ViewModel.StatusMessage))
        {
            _ = WorkOrderTextBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
    }
}