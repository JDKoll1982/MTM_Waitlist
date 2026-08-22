using System.Collections.Specialized;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkstationPage : Page
{
    // Responsive grid: card cell widths are recomputed from the grid's actual width so
    // the layout fills the available space and never clips at any resolution/window size.
    // The left photo column is fixed (300), so narrowing these also narrows the text panel.
    private const double MinItemWidth = 520;
    private const double MaxItemWidth = 580;
    private const double ItemHeight = 300;
    private const double ItemGap = 16; // card Margin of 8 on each side.

    public SetupWorkstationViewModel ViewModel { get; }

    public SetupWorkstationPage()
    {
        ViewModel = App.GetService<SetupWorkstationViewModel>();
        InitializeComponent();

        ViewModel.DisplayedHotWorkstations.CollectionChanged += OnWorkstationsCollectionChanged;
        ViewModel.DisplayedOtherWorkstations.CollectionChanged += OnWorkstationsCollectionChanged;
        Loaded += (_, _) => UpdateItemSize();
        Unloaded += (_, _) =>
        {
            ViewModel.DisplayedHotWorkstations.CollectionChanged -= OnWorkstationsCollectionChanged;
            ViewModel.DisplayedOtherWorkstations.CollectionChanged -= OnWorkstationsCollectionChanged;
        };
    }

    private void WorkstationGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateItemSize();
    }

    private void WorkstationCard_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SetupWorkstation workstation)
        {
            ViewModel.SelectedWorkstation = workstation;
        }
    }

    private void OnWorkstationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateItemSize();
    }

    private void UpdateItemSize()
    {
        if (HotWorkstationsGridView is null || OtherWorkstationsGridView is null)
        {
            return;
        }

        var availableWidth = Math.Max(HotWorkstationsGridView.ActualWidth, OtherWorkstationsGridView.ActualWidth);
        if (availableWidth <= 0)
        {
            return;
        }

        var columns = Math.Max(1, (int)Math.Floor((availableWidth + ItemGap) / (MinItemWidth + ItemGap)));
        var itemWidth = columns == 1
            ? Math.Min(availableWidth, MaxItemWidth)
            : Math.Clamp((availableWidth - (columns - 1) * ItemGap) / columns, MinItemWidth, MaxItemWidth);

        var hotPanel = HotWorkstationsGridView.ItemsPanelRoot;
        var otherPanel = OtherWorkstationsGridView.ItemsPanelRoot;
        ApplyItemSize(hotPanel, itemWidth);
        ApplyItemSize(otherPanel, itemWidth);

        // The items panels are realized lazily; retry after the next layout pass so the
        // responsive sizing still applies. Only retry for grids that are actually laid out
        // (width > 0); a collapsed/unrealized section must not spin this loop - its
        // SizeChanged fires again once it is expanded.
        if ((hotPanel is null && HotWorkstationsGridView.Items.Count > 0 && HotWorkstationsGridView.ActualWidth > 0)
            || (otherPanel is null && OtherWorkstationsGridView.Items.Count > 0 && OtherWorkstationsGridView.ActualWidth > 0))
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
