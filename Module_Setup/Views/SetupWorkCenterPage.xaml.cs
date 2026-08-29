using System.Collections.Specialized;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkCenterPage : Page
{
    // Responsive grid: card cell widths are recomputed from the grid's actual width so
    // the layout fills the available space and never clips at any resolution/window size.
    // The left photo column is fixed (300), so narrowing these also narrows the text panel.
    private const double MinItemWidth = 520;
    private const double MaxItemWidth = 580;
    private const double ItemHeight = 300;
    private const double ItemGap = 16; // card Margin of 8 on each side.

    public SetupWorkCenterViewModel ViewModel { get; }

    public SetupWorkCenterPage()
    {
        ViewModel = App.GetService<SetupWorkCenterViewModel>();
        InitializeComponent();

        ViewModel.DisplayedHotWorkCenters.CollectionChanged += OnWorkCentersCollectionChanged;
        ViewModel.DisplayedOtherWorkCenters.CollectionChanged += OnWorkCentersCollectionChanged;
        Loaded += (_, _) => UpdateItemSize();
        Unloaded += (_, _) =>
        {
            ViewModel.DisplayedHotWorkCenters.CollectionChanged -= OnWorkCentersCollectionChanged;
            ViewModel.DisplayedOtherWorkCenters.CollectionChanged -= OnWorkCentersCollectionChanged;
        };
    }

    private void WorkCenterGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateItemSize();
    }

    private void WorkCenterCard_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SetupWorkCenter workCenter)
        {
            ViewModel.SelectedWorkCenter = workCenter;
        }
    }

    private void OnWorkCentersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateItemSize();
    }

    private void UpdateItemSize()
    {
        if (HotWorkCentersGridView is null || OtherWorkCentersGridView is null)
        {
            return;
        }

        var availableWidth = Math.Max(HotWorkCentersGridView.ActualWidth, OtherWorkCentersGridView.ActualWidth);
        if (availableWidth <= 0)
        {
            return;
        }

        var columns = Math.Max(1, (int)Math.Floor((availableWidth + ItemGap) / (MinItemWidth + ItemGap)));
        var itemWidth = columns == 1
            ? Math.Min(availableWidth, MaxItemWidth)
            : Math.Clamp((availableWidth - (columns - 1) * ItemGap) / columns, MinItemWidth, MaxItemWidth);

        var hotPanel = HotWorkCentersGridView.ItemsPanelRoot;
        var otherPanel = OtherWorkCentersGridView.ItemsPanelRoot;
        ApplyItemSize(hotPanel, itemWidth);
        ApplyItemSize(otherPanel, itemWidth);

        // The items panels are realized lazily; retry after the next layout pass so the
        // responsive sizing still applies. Only retry for grids that are actually laid out
        // (width > 0); a collapsed/unrealized section must not spin this loop - its
        // SizeChanged fires again once it is expanded.
        if ((hotPanel is null && HotWorkCentersGridView.Items.Count > 0 && HotWorkCentersGridView.ActualWidth > 0)
            || (otherPanel is null && OtherWorkCentersGridView.Items.Count > 0 && OtherWorkCentersGridView.ActualWidth > 0))
        {
            DispatcherQueue.TryEnqueue(UpdateItemSize);
        }
    }

    private static void ApplyItemSize(UIElement? panel, double itemWidth)
    {
        if (panel is ItemsWrapGrid wrapGrid)
        {
            wrapGrid.ItemWidth = itemWidth;
            wrapGrid.ItemHeight = ItemHeight;
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
