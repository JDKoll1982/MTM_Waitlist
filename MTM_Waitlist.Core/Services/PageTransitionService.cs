using Microsoft.UI.Xaml.Media.Animation;

using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class PageTransitionService : IPageTransitionService
{
    public NavigationTransitionInfo GetForNavigation(object? parameter, bool clearNavigation)
    {
        return parameter is not null && !clearNavigation
            ? new DrillInNavigationTransitionInfo()
            : new EntranceNavigationTransitionInfo();
    }

    public NavigationTransitionInfo GetForBackNavigation()
    {
        return new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromLeft
        };
    }
}
