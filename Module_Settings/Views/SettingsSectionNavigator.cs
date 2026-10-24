using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// Takes a reader who arrived from the settings hub's search to the exact section they searched for.
/// </summary>
/// <remarks>
/// A page is long enough that landing at its top would leave the reader scrolling and guessing, which is the same as
/// not having searched at all. The section key travels as the navigation parameter; this type opens whatever
/// collapsed sections sit around it, then scrolls it into view.
/// </remarks>
internal static class SettingsSectionNavigator
{
    /// <summary>Opens and scrolls to the section named by the navigation parameter, when there is one.</summary>
    /// <param name="page">The page that has just been navigated to.</param>
    /// <param name="parameter">The navigation parameter; anything but a section key is ignored.</param>
    internal static void BringIntoView(Page page, object? parameter)
    {
        if (parameter is not string sectionKey || string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        if (page.IsLoaded)
        {
            Queue(page, sectionKey);
            return;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            page.Loaded -= OnLoaded;
            Queue(page, sectionKey);
        }

        page.Loaded += OnLoaded;
    }

    /// <summary>
    /// Queues the reveal one beat later than the navigation.
    /// </summary>
    /// <remarks>
    /// Two low-priority hops: the first lands after the page's own load work, the second after the panels that were
    /// loaded for this reader have been measured and can be scrolled to. A single hop ran before the panels had a
    /// layout, and the scroll then went nowhere.
    /// </remarks>
    private static void Queue(Page page, string sectionKey)
    {
        page.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => page.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () => Reveal(page, sectionKey)));
    }

    private static void Reveal(Page page, string sectionKey)
    {
        var target = Find(page, sectionKey);

        if (target is null)
        {
            // A section inside a collapsed expander can be unrealised in some states. Opening the page's expanders
            // and looking again is better than silently landing at the top of the page.
            OpenEveryExpander(page);
            page.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                var retried = Find(page, sectionKey);
                if (retried is null)
                {
                    return;
                }

                OpenAncestors(retried, page);
                retried.StartBringIntoView();
            });

            return;
        }

        OpenAncestors(target, page);
        target.StartBringIntoView();
    }

    /// <summary>Finds a section by its <c>x:Name</c>, or by the automation id the markup gives it.</summary>
    private static FrameworkElement? Find(Page page, string sectionKey)
    {
        if (page.FindName(sectionKey) is FrameworkElement named)
        {
            return named;
        }

        return Descendants(page).FirstOrDefault(
            element => AutomationProperties.GetAutomationId(element) == sectionKey);
    }

    /// <summary>Expands every settings expander on the page.</summary>
    private static void OpenEveryExpander(DependencyObject root)
    {
        foreach (var element in Descendants(root))
        {
            if (element is SettingsExpander expander)
            {
                expander.IsExpanded = true;
            }
        }
    }

    /// <summary>Expands the settings expanders a section sits inside.</summary>
    private static void OpenAncestors(DependencyObject element, Page page)
    {
        var parent = VisualTreeHelper.GetParent(element);

        while (parent is not null && !ReferenceEquals(parent, page))
        {
            if (parent is SettingsExpander expander)
            {
                expander.IsExpanded = true;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }
    }

    private static IEnumerable<FrameworkElement> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);

        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            if (child is FrameworkElement element)
            {
                yield return element;
            }

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
