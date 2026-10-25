using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// Keeps a wrapping card grid as wide as the container it sits in: as many columns as fit, each card as wide as its
/// share of the width. The card's <b>height is left alone</b> — it is declared on the grid's own panel, where it
/// always was.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> Three pages used to carry their own copy of the column arithmetic in code-behind — a fixed
/// column count, a <c>SizeChanged</c> handler, a collection-changed hook and a retry loop for a panel that had not
/// been built yet. Three copies of one rule drift, and the copies disagreed: two pages sized from the widest of two
/// grids, one sized from its own, and one subtracted a pixel the others did not.
/// </para>
/// <para>
/// <b>What it does instead.</b> The page declares the narrowest card worth drawing and nothing else; the width and
/// the column count come from the container on every measure. Declaring the behaviour twice on one grid is harmless
/// — the handlers are attached once.
/// </para>
/// <para>
/// <b>It is an attached behaviour, not a service, because the thing it changes is the panel of a control the page
/// owns.</b> Nothing else can reach that panel without the page handing it over, and a page that has to hand it over
/// is a page that still owns the arithmetic.
/// </para>
/// </remarks>
public static class CardGridSizing
{
    /// <summary>The narrowest card the grid may draw, in effective pixels.</summary>
    public static readonly DependencyProperty MinimumCardWidthProperty =
        DependencyProperty.RegisterAttached(
            "MinimumCardWidth",
            typeof(double),
            typeof(CardGridSizing),
            new PropertyMetadata(320d, OnSizingChanged));

    /// <summary>The space between two cards, and between the cards and the grid's edges.</summary>
    public static readonly DependencyProperty GapProperty =
        DependencyProperty.RegisterAttached(
            "Gap",
            typeof(double),
            typeof(CardGridSizing),
            new PropertyMetadata(CardGridLayout.DefaultGap, OnSizingChanged));

    /// <summary>A ceiling on the columns, or zero for as many as fit.</summary>
    public static readonly DependencyProperty MaximumColumnsProperty =
        DependencyProperty.RegisterAttached(
            "MaximumColumns",
            typeof(int),
            typeof(CardGridSizing),
            new PropertyMetadata(0, OnSizingChanged));

    /// <summary>Whether this grid has already been wired to the layout events.</summary>
    private static readonly DependencyProperty IsWiredProperty =
        DependencyProperty.RegisterAttached(
            "IsWired",
            typeof(bool),
            typeof(CardGridSizing),
            new PropertyMetadata(false));

    /// <summary>Whether a retry for an unbuilt items panel is already queued.</summary>
    private static readonly DependencyProperty IsRetryQueuedProperty =
        DependencyProperty.RegisterAttached(
            "IsRetryQueued",
            typeof(bool),
            typeof(CardGridSizing),
            new PropertyMetadata(false));

    public static double GetMinimumCardWidth(DependencyObject element) =>
        (double)element.GetValue(MinimumCardWidthProperty);

    public static void SetMinimumCardWidth(DependencyObject element, double value) =>
        element.SetValue(MinimumCardWidthProperty, value);

    public static double GetGap(DependencyObject element) => (double)element.GetValue(GapProperty);

    public static void SetGap(DependencyObject element, double value) => element.SetValue(GapProperty, value);

    public static int GetMaximumColumns(DependencyObject element) => (int)element.GetValue(MaximumColumnsProperty);

    public static void SetMaximumColumns(DependencyObject element, int value) =>
        element.SetValue(MaximumColumnsProperty, value);

    private static void OnSizingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListViewBase grid)
        {
            return;
        }

        if (!(bool)grid.GetValue(IsWiredProperty))
        {
            grid.SetValue(IsWiredProperty, true);

            // Loaded rather than the constructor: the items panel is built during the first layout pass, and a
            // grid measured before that reports no panel at all.
            grid.Loaded += (sender, _) => Apply(sender as ListViewBase);
            grid.SizeChanged += (sender, _) => Apply(sender as ListViewBase);
        }

        Apply(grid);
    }

    /// <summary>
    /// Sizes the grid's panel from the width the grid has right now.
    /// </summary>
    /// <remarks>
    /// A panel that has not been built yet is retried once after the next layout pass, and only while the grid has
    /// a width and holds something: a collapsed section must not spin this, because its own <c>SizeChanged</c>
    /// fires when it is shown again.
    /// </remarks>
    private static void Apply(ListViewBase? grid)
    {
        if (grid is null || grid.ActualWidth <= 0)
        {
            return;
        }

        if (grid.ItemsPanelRoot is not ItemsWrapGrid panel)
        {
            QueueRetry(grid);
            return;
        }

        var solution = CardGridLayout.Solve(
            grid.ActualWidth,
            GetMinimumCardWidth(grid),
            GetGap(grid),
            GetMaximumColumns(grid));

        if (!solution.IsSolved)
        {
            return;
        }

        panel.ItemWidth = solution.CardWidth;

        // The height is deliberately not touched: it stays whatever the grid's own panel declares, so the rows keep
        // the height the page chose rather than following the container vertically.
    }

    private static void QueueRetry(ListViewBase grid)
    {
        if ((bool)grid.GetValue(IsRetryQueuedProperty) || grid.Items.Count == 0)
        {
            return;
        }

        grid.SetValue(IsRetryQueuedProperty, true);

        var queue = grid.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        queue?.TryEnqueue(() =>
        {
            grid.SetValue(IsRetryQueuedProperty, false);
            Apply(grid);
        });
    }
}
