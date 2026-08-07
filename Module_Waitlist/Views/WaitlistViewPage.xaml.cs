using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class WaitlistViewPage : Page
{
    public WaitlistViewViewModel ViewModel
    {
        get;
    }

    public WaitlistViewPage()
    {
        ViewModel = App.GetService<WaitlistViewViewModel>();
        InitializeComponent();
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (ViewModel.ItemClickCommand != null && ViewModel.ItemClickCommand.CanExecute(e.ClickedItem))
        {
            ViewModel.ItemClickCommand.Execute(e.ClickedItem);
        }
    }

    private async void OnAddRequestClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialogService = App.GetService<IWorkCenterSelectionDialogService>();
        if (XamlRoot is null)
        {
            return;
        }

        _ = await dialogService.ShowForCurrentWorkstationAsync(XamlRoot).ConfigureAwait(true);
    }
}
