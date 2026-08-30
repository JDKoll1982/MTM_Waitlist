using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Shared.Services;

public static class TooltipBehavior
{
    private static readonly DependencyProperty IsApplyScheduledProperty =
        DependencyProperty.RegisterAttached(
            "IsApplyScheduled",
            typeof(bool),
            typeof(TooltipBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ResourceKeyProperty =
        DependencyProperty.RegisterAttached(
            "ResourceKey",
            typeof(string),
            typeof(TooltipBehavior),
            new PropertyMetadata(null, OnTooltipMetadataChanged));

    public static readonly DependencyProperty AssociatedFilesProperty =
        DependencyProperty.RegisterAttached(
            "AssociatedFiles",
            typeof(string),
            typeof(TooltipBehavior),
            new PropertyMetadata(null, OnTooltipMetadataChanged));

    public static readonly DependencyProperty FallbackTextProperty =
        DependencyProperty.RegisterAttached(
            "FallbackText",
            typeof(string),
            typeof(TooltipBehavior),
            new PropertyMetadata(null, OnTooltipMetadataChanged));

    public static string GetResourceKey(DependencyObject obj) => (string)obj.GetValue(ResourceKeyProperty);

    public static void SetResourceKey(DependencyObject obj, string value) => obj.SetValue(ResourceKeyProperty, value);

    public static string GetAssociatedFiles(DependencyObject obj) => (string)obj.GetValue(AssociatedFilesProperty);

    public static void SetAssociatedFiles(DependencyObject obj, string value) => obj.SetValue(AssociatedFilesProperty, value);

    public static string GetFallbackText(DependencyObject obj) => (string)obj.GetValue(FallbackTextProperty);

    public static void SetFallbackText(DependencyObject obj, string value) => obj.SetValue(FallbackTextProperty, value);

    private static bool GetIsApplyScheduled(DependencyObject obj) => (bool)obj.GetValue(IsApplyScheduledProperty);

    private static void SetIsApplyScheduled(DependencyObject obj, bool value) => obj.SetValue(IsApplyScheduledProperty, value);

    private static void OnTooltipMetadataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            ScheduleTooltipApply(element);
        }
    }

    private static void ScheduleTooltipApply(FrameworkElement element)
    {
        // Avoid resolving resources/DI while XAML is still parsing during startup.
        if (element.IsLoaded)
        {
            ApplyTooltip(element);
            return;
        }

        if (GetIsApplyScheduled(element))
        {
            return;
        }

        SetIsApplyScheduled(element, true);
        element.Loaded += OnElementLoaded;
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnElementLoaded;
        SetIsApplyScheduled(element, false);
        ApplyTooltip(element);
    }

    private static void ApplyTooltip(FrameworkElement element)
    {
        try
        {
            var resourceKey = GetResourceKey(element);
            var associatedFiles = ParseFiles(GetAssociatedFiles(element));
            var fallbackText = GetFallbackText(element);
            var tooltipService = SharedServiceLocator.TooltipService;
            if (tooltipService is null)
            {
                return;
            }

            tooltipService.ApplyToElement(element, resourceKey, associatedFiles, fallbackText);

            // Track for developer inspector (Ctrl+Alt+Right-Click) without creating DI cycles.
            SharedServiceLocator.ControlInspectorService?.TrackElement(element, resourceKey, associatedFiles, fallbackText);
        }
        catch (Exception)
        {
            // Never allow tooltip wiring to break page construction/startup.
        }
    }

    private static IEnumerable<string> ParseFiles(string? files)
    {
        if (string.IsNullOrWhiteSpace(files))
        {
            return Array.Empty<string>();
        }

        return files.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
