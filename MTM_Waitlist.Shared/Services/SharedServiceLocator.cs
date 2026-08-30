namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// Static accessor for XAML-instantiated types (behaviors) that cannot receive DI.
/// The app sets these once during startup.
/// </summary>
public static class SharedServiceLocator
{
    public static ITooltipService? TooltipService { get; set; }

    public static IControlInspectorService? ControlInspectorService { get; set; }
}
