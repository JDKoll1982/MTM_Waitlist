using System.Collections.Specialized;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestWorkCenterPage : Page
{
    // Unified grid: always three card columns, each a third of the container's width so the grid
    // fills the collapsible container edge to edge and stays centred.
    private const int ColumnCount = 3;
    private const double ItemHeight = 190;

    public NewRequestWorkCenterViewModel ViewModel
    {
        get;
    }

    public NewRequestWorkCenterPage()
    {
        ViewModel = App.GetService<NewRequestWorkCenterViewModel>();
        InitializeComponent();

        ViewModel.HotWorkCenters.CollectionChanged += OnWorkCentersCollectionChanged;
        ViewModel.OtherWorkCenters.CollectionChanged += OnWorkCentersCollectionChanged;
        Loaded += (_, _) => UpdateItemSize();
        Unloaded += (_, _) =>
        {
            ViewModel.HotWorkCenters.CollectionChanged -= OnWorkCentersCollectionChanged;
            ViewModel.OtherWorkCenters.CollectionChanged -= OnWorkCentersCollectionChanged;
        };
    }

    /// <summary>
    /// Rebuilds the building flyout's cards as it opens, so the building in effect is never among them (FR-022).
    /// </summary>
    private void BuildingFlyout_Opening(object? sender, object e) => ViewModel.RefreshBuildingCards();

    /// <summary>Chooses the building whose card was clicked, which is the same choice the drop-down used to make.</summary>
    private void BuildingCard_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BuildingCardOption card)
        {
            ViewModel.SelectBuilding(card.Building);
        }
    }

    private void WorkCenterGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateItemSize();
    }

    private void HotWorkCentersGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WorkCenterSelectionItem item)
        {
            ViewModel.SelectWorkCenterCommand.Execute(item);
        }
    }

    private void OtherWorkCentersGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WorkCenterSelectionItem item)
        {
            ViewModel.SelectWorkCenterCommand.Execute(item);
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

        var itemWidth = Math.Floor(availableWidth / ColumnCount) - 1;

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
}
