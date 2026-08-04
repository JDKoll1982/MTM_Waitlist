using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Controls;

public partial class WaitlistLineCardView : UserControl
{
    public static readonly DependencyProperty OrderProperty = DependencyProperty.Register(
        nameof(Order),
        typeof(SampleOrder),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

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
}