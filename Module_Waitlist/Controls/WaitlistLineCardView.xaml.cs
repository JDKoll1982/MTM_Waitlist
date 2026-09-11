using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Text;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using System.ComponentModel;

namespace MTM_Waitlist.Module_Waitlist.Controls;

public partial class WaitlistLineCardView : UserControl
{
    public static readonly DependencyProperty OrderProperty = DependencyProperty.Register(
        nameof(Order),
        typeof(SampleOrder),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null, OnOrderChanged));

    public static readonly DependencyProperty RemainingTimeBrushProperty = DependencyProperty.Register(
        nameof(RemainingTimeBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(new SolidColorBrush(Colors.MediumSeaGreen)));

    public static readonly DependencyProperty RemainingTimeFontWeightProperty = DependencyProperty.Register(
        nameof(RemainingTimeFontWeight),
        typeof(Windows.UI.Text.FontWeight),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(FontWeights.Normal));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty AccentSurfaceBrushProperty = DependencyProperty.Register(
        nameof(AccentSurfaceBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty BadgeBackgroundBrushProperty = DependencyProperty.Register(
        nameof(BadgeBackgroundBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty BadgeTextProperty = DependencyProperty.Register(
        nameof(BadgeText),
        typeof(string),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailsContentProperty = DependencyProperty.Register(
        nameof(DetailsContent),
        typeof(object),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    private SampleOrder? _subscribedOrder;

    public WaitlistLineCardView()
    {
        InitializeComponent();

        // The countdown's colour and weight are derived from the row's own texts, so the card must listen
        // for the once-a-minute refresh as well as for a change of row. Re-subscribing on Loaded keeps
        // that working when the ListView recycles this container.
        Loaded += OnCardLoaded;
        Unloaded += OnCardUnloaded;
    }

    private void OnCardLoaded(object sender, RoutedEventArgs e) => AttachOrder(Order);

    private void OnCardUnloaded(object sender, RoutedEventArgs e) => DetachOrder(_subscribedOrder);

    private void AttachOrder(SampleOrder? order)
    {
        if (order is null || ReferenceEquals(_subscribedOrder, order))
        {
            return;
        }

        order.PropertyChanged += OnOrderPropertyChanged;
        _subscribedOrder = order;
    }

    private void DetachOrder(SampleOrder? order)
    {
        if (order is null || !ReferenceEquals(_subscribedOrder, order))
        {
            return;
        }

        order.PropertyChanged -= OnOrderPropertyChanged;
        _subscribedOrder = null;
    }

    private void OnOrderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SampleOrder.RemainingTimeText), System.StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(SampleOrder.IsOverdue), System.StringComparison.Ordinal))
        {
            UpdateRemainingTimeBrush();
        }
    }

    public SampleOrder? Order
    {
        get => (SampleOrder?)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    public Brush RemainingTimeBrush
    {
        get => (Brush)GetValue(RemainingTimeBrushProperty);
        set => SetValue(RemainingTimeBrushProperty, value);
    }

    public Windows.UI.Text.FontWeight RemainingTimeFontWeight
    {
        get => (Windows.UI.Text.FontWeight)GetValue(RemainingTimeFontWeightProperty);
        set => SetValue(RemainingTimeFontWeightProperty, value);
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush? AccentSurfaceBrush
    {
        get => (Brush?)GetValue(AccentSurfaceBrushProperty);
        set => SetValue(AccentSurfaceBrushProperty, value);
    }

    public Brush? BadgeBackgroundBrush
    {
        get => (Brush?)GetValue(BadgeBackgroundBrushProperty);
        set => SetValue(BadgeBackgroundBrushProperty, value);
    }

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public object? DetailsContent
    {
        get => GetValue(DetailsContentProperty);
        set => SetValue(DetailsContentProperty, value);
    }

    private static void OnOrderChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WaitlistLineCardView control)
        {
            // Drop the previous row's subscription before taking the new one, or a recycled container
            // would keep repainting a row it no longer shows.
            control.DetachOrder(args.OldValue as SampleOrder);
            control.AttachOrder(args.NewValue as SampleOrder);
            control.UpdateRemainingTimeBrush();
        }
    }

    private void UpdateRemainingTimeBrush()
    {
        var remainingTimeText = Order?.RemainingTimeText;
        var isOverdue = Order?.IsOverdue == true;

        // One palette for the card and the details page, so the two can never disagree about what "red" means.
        RemainingTimeBrush = RemainingTimePalette.For(remainingTimeText, isOverdue);
        RemainingTimeFontWeight = isOverdue || RemainingTimePalette.IsOverdueText(remainingTimeText)
            ? FontWeights.Bold
            : FontWeights.Normal;
    }
}