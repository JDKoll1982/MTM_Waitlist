using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Static accessor used by XAML-instantiated types (behaviors) that cannot
/// receive DI. The app sets <see cref="NavigationService"/> once during startup.
/// </summary>
public static class AppServiceLocator
{
    public static INavigationService? NavigationService { get; set; }
}
