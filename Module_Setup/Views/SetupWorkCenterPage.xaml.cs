using System.Collections.Specialized;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

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
    private async void WorkCenterCard_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
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

        if (await RefuseGenericSetupAsync(workCenter))
        {
            return;
        }

        if (ViewModel.ContinueCommand.CanExecute(null))
        {
            ViewModel.ContinueCommand.Execute(null);
        }
    }

    /// <summary>
    /// The Continue button, held here rather than bound straight to the command so a generic work centre can be
    /// refused before the workflow moves on.
    /// </summary>
    private async void OnContinueClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWorkCenter is { } workCenter && await RefuseGenericSetupAsync(workCenter))
        {
            return;
        }

        if (ViewModel.ContinueCommand.CanExecute(null))
        {
            ViewModel.ContinueCommand.Execute(null);
        }
    }

    /// <summary>
    /// Says why a generic work centre cannot be set up, and answers whether it was one.
    /// </summary>
    /// <remarks>
    /// A generic work centre raises requests without a work order, so there is no job to set up there and the next
    /// step would have nothing to read. The refusal is stated rather than shown as a disabled card: the person needs
    /// to know why the work centre they chose will not open.
    /// </remarks>
    private static async Task<bool> RefuseGenericSetupAsync(SetupWorkCenter workCenter)
    {
        if (!workCenter.IsGeneric)
        {
            return false;
        }

        if (App.MainWindow is { } owner)
        {
            await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
            {
                Kind = MessageKind.Acknowledge,
                Title = "Generic work center",
                Message = $"{workCenter.Name} is a generic work center. Requests raised there do not use a work order, "
                    + "so there is no setup to perform.",
                CancelLabel = "Close",
            });
        }

        return true;
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
        ViewModel.WorkCenterIsGenericInput = false;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            PlaceholderText = "Enter workstation name",
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(ViewModel.BuildingInput);
        var genericInput = CreateGenericInput(isGeneric: false);

        if (App.MainWindow is not { } owner)
        {
            return;
        }

        var dialog = CreateWorkCenterDialog(owner, "New Workstation", nameInput, buildingInput, genericInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkCenterNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            ViewModel.WorkCenterIsGenericInput = genericInput.IsOn;
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
        ViewModel.WorkCenterIsGenericInput = workstation.IsGeneric;

        var nameInput = new TextBox
        {
            Header = "Workstation",
            Text = workstation.Name,
            MinWidth = 320,
        };
        var buildingInput = CreateBuildingComboBox(workstation.Building);
        var genericInput = CreateGenericInput(workstation.IsGeneric);

        if (App.MainWindow is not { } owner)
        {
            return;
        }

        var dialog = CreateWorkCenterDialog(owner, "Edit Workstation", nameInput, buildingInput, genericInput);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.WorkCenterNameInput = nameInput.Text;
            ViewModel.BuildingInput = buildingInput.SelectedItem as string ?? string.Empty;
            ViewModel.WorkCenterIsGenericInput = genericInput.IsOn;
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

    /// <summary>
    /// The designation control, offered inside the work center form rather than on the page (016 FR-001).
    /// </summary>
    /// <remarks>
    /// It lives here because the mark belongs to the work center being saved: a page-level switch would write the
    /// moment it was flipped, before the person had finished editing the row it describes. Both callers are already
    /// behind the <c>CanManageWorkCenters</c> gate, so the control stays absent for anyone without the permission
    /// rather than greyed out (FR-002).
    /// </remarks>
    private static ToggleSwitch CreateGenericInput(bool isGeneric)
    {
        var toggle = new ToggleSwitch
        {
            Header = "Generic work center",
            OnContent = "Requests raised here use the generic flow: no work order",
            OffContent = "Requests raised here follow the work-order flow",
            IsOn = isGeneric,
            MinWidth = 320,
        };

        AutomationProperties.SetAutomationId(toggle, "SetupWorkCenterDialog_GenericToggle");

        return toggle;
    }

    private DevWinUI.WindowedContentDialog CreateWorkCenterDialog(
        Window owner,
        string title,
        TextBox nameInput,
        ComboBox buildingInput,
        ToggleSwitch genericInput)
    {
        var content = new StackPanel { Spacing = 12, MinWidth = DialogSizing.MinWidth };
        content.Children.Add(nameInput);
        content.Children.Add(buildingInput);
        content.Children.Add(genericInput);

        return WindowedDialogHost.Create(owner, title, content, "Save", "Cancel", MessageDialogButton.Act);
    }
}
