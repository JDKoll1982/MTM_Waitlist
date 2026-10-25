using System.Diagnostics.CodeAnalysis;

using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Services;

public class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;

    private readonly IPageService _pageService;
    private readonly ISetupWorkflowService _setupWorkflowService;

    private NavigationView? _navigationView;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public NavigationViewService(INavigationService navigationService, IPageService pageService, ISetupWorkflowService setupWorkflowService)
    {
        _navigationService = navigationService;
        _pageService = pageService;
        _setupWorkflowService = setupWorkflowService;
    }

    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
    }

    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => _navigationService.GoBack();

    private async void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            var pageKey = string.Empty;

            if (args.IsSettingsInvoked)
            {
                AppLog.Info("NavigationViewService", "Settings item invoked.");
                pageKey = typeof(SettingsViewModel).FullName!;
            }
            else
            {
                var selectedItem = args.InvokedItemContainer as NavigationViewItem;
                if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is not string resolvedPageKey || string.IsNullOrWhiteSpace(resolvedPageKey))
                {
                    AppLog.Info("NavigationViewService", "Item invoked without a valid navigation key.");
                    return;
                }

                pageKey = resolvedPageKey;
            }

            AppLog.Info("NavigationViewService", $"Navigation requested. PageKey='{pageKey}'.");

            if (!await CanLeaveSetupAsync(sender, pageKey).ConfigureAwait(true))
            {
                ReselectCurrentNavigationItem(sender);
                return;
            }

            _navigationService.NavigateTo(pageKey);
        }
        catch (Exception ex)
        {
            AppLog.Error("NavigationViewService", ex, "NavigationView item invocation failed.");
        }
    }

    private async Task<bool> CanLeaveSetupAsync(NavigationView sender, string destinationPageKey)
    {
        var currentPageType = _navigationService.Frame?.Content?.GetType();
        var currentPageNamespace = currentPageType?.Namespace ?? string.Empty;
        if (!currentPageNamespace.StartsWith("MTM_Waitlist.Module_Setup.Views", StringComparison.Ordinal))
        {
            return true;
        }

        var destinationPageType = _pageService.GetPageType(destinationPageKey);
        var destinationPageNamespace = destinationPageType.Namespace ?? string.Empty;
        if (destinationPageNamespace.StartsWith("MTM_Waitlist.Module_Setup.Views", StringComparison.Ordinal))
        {
            return true;
        }

        if (!_setupWorkflowService.HasUnsavedChanges)
        {
            return true;
        }

        if (App.MainWindow is not { } owner)
        {
            // No window to own the dialog; leaving is refused rather than assumed.
            return false;
        }

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.LeaveWithoutSaving,
            Title = LocalizeOrDefault("Setup_Navigation_LeaveSetup.Title", "Leave Work Center Setup?"),
            Message = LocalizeOrDefault("Setup_Navigation_LeaveSetup.Message", "All unsaved setup data will be lost. Continue?"),
            ActLabel = LocalizeOrDefault("Setup_Navigation_LeaveSetup.Confirm", "Yes"),
            CancelLabel = LocalizeOrDefault("Setup_Navigation_LeaveSetup.Cancel", "No"),
        });

        if (!outcome.IsAct)
        {
            return false;
        }

        await _setupWorkflowService.ResetAsync().ConfigureAwait(true);
        return true;
    }

    private void ReselectCurrentNavigationItem(NavigationView sender)
    {
        var currentPageType = _navigationService.Frame?.Content?.GetType();
        if (currentPageType is null)
        {
            return;
        }

        var selectedItem = GetSelectedItem(currentPageType);
        if (selectedItem is not null)
        {
            sender.SelectedItem = selectedItem;
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }
}
