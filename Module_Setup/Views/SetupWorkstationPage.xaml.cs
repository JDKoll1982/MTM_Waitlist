using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkstationPage : Page
{
    public SetupWorkstationViewModel ViewModel { get; }

    public SetupWorkstationPage()
    {
        ViewModel = App.GetService<SetupWorkstationViewModel>();
        InitializeComponent();
    }

    private async void OnNewWorkstationClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkstations)
        {
            return;
        }

        ViewModel.SelectedWorkstation = null;
        ViewModel.WorkstationNameInput = string.Empty;
        ViewModel.BuildingInput = ViewModel.Buildings.FirstOrDefault() ?? string.Empty;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            PlaceholderText = "Enter workstation name",
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(ViewModel.BuildingInput);

        var dialog = CreateWorkstationDialog("New Workstation", nameInput, buildingInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkstationNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            if (ViewModel.AddWorkstationCommand.CanExecute(null))
            {
                await ViewModel.AddWorkstationCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnEditWorkstationClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkstations || sender is not MenuFlyoutItem { Tag: SetupWorkstation workstation })
        {
            return;
        }

        ViewModel.SelectedWorkstation = workstation;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            Text = workstation.Name,
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(workstation.Building);

        var dialog = CreateWorkstationDialog("Edit Workstation", nameInput, buildingInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkstationNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            if (ViewModel.UpdateWorkstationCommand.CanExecute(null))
            {
                await ViewModel.UpdateWorkstationCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnRemoveWorkstationClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkstations || sender is not MenuFlyoutItem { Tag: SetupWorkstation workstation })
        {
            return;
        }

        ViewModel.SelectedWorkstation = workstation;
        if (ViewModel.RemoveWorkstationCommand.CanExecute(null))
        {
            await ViewModel.RemoveWorkstationCommand.ExecuteAsync(null);
        }
    }

    private ComboBox CreateBuildingComboBox(string selectedBuilding)
    {
        return new ComboBox
        {
            Header = "Building",
            ItemsSource = ViewModel.Buildings,
            SelectedItem = selectedBuilding,
            MinWidth = 320,
        };
    }

    private ContentDialog CreateWorkstationDialog(string title, TextBox nameInput, ComboBox buildingInput)
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(nameInput);
        content.Children.Add(buildingInput);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        return dialog;
    }
}
