using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Text;
using MTM_Waitlist.Module_Waitlist.Models;

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

    public WaitlistLineCardView()
    {
        InitializeComponent();
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
            control.UpdateRemainingTimeBrush();
        }
    }

    private void UpdateRemainingTimeBrush()
    {
        var remainingTimeText = Order?.RemainingTimeText;
        var isOverdue = Order?.IsOverdue == true
            || string.Equals(remainingTimeText?.Trim(), "Overdue", System.StringComparison.OrdinalIgnoreCase);

        if (isOverdue)
        {
            RemainingTimeBrush = new SolidColorBrush(Colors.IndianRed);
            RemainingTimeFontWeight = FontWeights.Bold;
            return;
        }

        RemainingTimeFontWeight = FontWeights.Normal;

        if (string.IsNullOrWhiteSpace(remainingTimeText) || !TimeSpan.TryParse(remainingTimeText, out var parsedRemainingTime))
        {
            RemainingTimeBrush = new SolidColorBrush(Colors.MediumSeaGreen);
            return;
        }

        var minutesRemaining = parsedRemainingTime.TotalMinutes;
        if (minutesRemaining <= 15)
        {
            RemainingTimeBrush = new SolidColorBrush(Colors.IndianRed);
            return;
        }

        if (minutesRemaining <= 30)
        {
            RemainingTimeBrush = new SolidColorBrush(Colors.Goldenrod);
            return;
        }

        RemainingTimeBrush = new SolidColorBrush(Colors.MediumSeaGreen);
    }
}