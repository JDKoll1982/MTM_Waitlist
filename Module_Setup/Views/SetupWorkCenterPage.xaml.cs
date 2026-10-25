using System.Collections.Specialized;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkCenterPage : Page
{
    public SetupWorkCenterViewModel ViewModel { get; }

    public SetupWorkCenterPage()
    {
        ViewModel = App.GetService<SetupWorkCenterViewModel>();
        InitializeComponent();
    }

    private void WorkCenterCard_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SetupWorkCenter workCenter)
        {
            ViewModel.SelectedWorkCenter = workCenter;
        }
    }

    /// <summary>
    /// A double-click on a card is the Continue button's own gesture: the person has chosen that work centre and
    /// wants to move on, so the second click selects and continues rather than only re-selecting (FR-002).
    /// </summary>
    private void WorkCenterCard_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // A double-click on the card's history button belongs to that button, not to the card.
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (FindCard(e.OriginalSource as DependencyObject) is not { } workCenter)
        {
            return;
        }

        ViewModel.SelectedWorkCenter = workCenter;

        if (ViewModel.ContinueCommand.CanExecute(null))
        {
            ViewModel.ContinueCommand.Execute(null);
        }
    }

    /// <summary>
    /// Finds the work-centre card a double-click landed on. A double-click inside a card lands on the text or the
    /// picture, not on the grid item, so the visual tree is walked up to the container that holds the card's data.
    /// </summary>
    private static SetupWorkCenter? FindCard(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is GridViewItem { Content: SetupWorkCenter workCenter })
            {
                return workCenter;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    /// <summary>Finds the nearest ancestor of a type, so a control inside the card can claim its own gesture.</summary>
    private static T? FindAncestor<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void OnViewHistoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: SetupWorkCenter workCenter })
        {
            ViewModel.ViewHistoryCommand.Execute(workCenter);
        }
    }

    private async void OnNewWorkCenterClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkCenters)
        {
            return;
        }

        ViewModel.SelectedWorkCenter = null;
        ViewModel.WorkCenterNameInput = string.Empty;
        ViewModel.BuildingInput = ViewModel.Buildings.FirstOrDefault() ?? string.Empty;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            PlaceholderText = "Enter workstation name",
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(ViewModel.BuildingInput);

        var dialog = CreateWorkCenterDialog("New Workstation", nameInput, buildingInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkCenterNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            if (ViewModel.AddWorkCenterCommand.CanExecute(null))
            {
                await ViewModel.AddWorkCenterCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnEditWorkCenterClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkCenters || sender is not MenuFlyoutItem { Tag: SetupWorkCenter workstation })
        {
            return;
        }

        ViewModel.SelectedWorkCenter = workstation;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            Text = workstation.Name,
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(workstation.Building);

        var dialog = CreateWorkCenterDialog("Edit Workstation", nameInput, buildingInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkCenterNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            if (ViewModel.UpdateWorkCenterCommand.CanExecute(null))
            {
                await ViewModel.UpdateWorkCenterCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnRemoveWorkCenterClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanManageWorkCenters || sender is not MenuFlyoutItem { Tag: SetupWorkCenter workstation })
        {
            return;
        }

        ViewModel.SelectedWorkCenter = workstation;
        if (ViewModel.RemoveWorkCenterCommand.CanExecute(null))
        {
            await ViewModel.RemoveWorkCenterCommand.ExecuteAsync(null);
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

    private ContentDialog CreateWorkCenterDialog(string title, TextBox nameInput, ComboBox buildingInput)
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
