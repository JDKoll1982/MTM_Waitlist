using System.Collections.Specialized;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestWorkCenterPage : Page
{
    // Responsive grid: card cell widths are recomputed from the grid's actual width so
    // the layout fills the available space and never clips at any resolution/window size.
    // The left photo column is fixed (300), so narrowing these also narrows the text panel.
    private const double MinItemWidth = 520;
    private const double MaxItemWidth = 580;
    private const double ItemHeight = 300;
    private const double ItemGap = 16; // card Margin of 8 on each side.

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
}
