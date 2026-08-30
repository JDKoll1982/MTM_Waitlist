using Microsoft.UI.Xaml.Media.Animation;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IPageTransitionService
{
    NavigationTransitionInfo GetForNavigation(object? parameter, bool clearNavigation);

    NavigationTransitionInfo GetForBackNavigation();
}
