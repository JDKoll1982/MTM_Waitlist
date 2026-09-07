using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Services.MockMode;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

using Windows.System;

namespace MTM_Waitlist.Module_Core.Views;

// TODO: Update NavigationViewItem titles and icons in ShellPage.xaml.
public sealed partial class ShellPage : Page
{
    private readonly IStartupShellStateService _startupShellStateService;
    private readonly MockModeToastCoordinator _mockModeToastCoordinator;

    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(
        ShellViewModel viewModel,
        IStartupShellStateService startupShellStateService,
        MockModeToastCoordinator mockModeToastCoordinator)
    {
        ViewModel = viewModel;
        _startupShellStateService = startupShellStateService;
        _mockModeToastCoordinator = mockModeToastCoordinator;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
        NavigationFrame.Navigated += NavigationFrame_Navigated;
        _startupShellStateService.StateChanged += OnStartupShellStateChanged;
        ApplyStartupShellState();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.RefreshUserInfo();
        SyncMyRequestsVisibility();

        // Start the mock-mode polling host (central mock-config refresh + taskbar toasts on state change)
        // once the shell is up and the signed-in role is known. Unpackaged apps are a no-op.
        _mockModeToastCoordinator.Start();

        if (AppTitleBar is null || AppTitleBarText is null)
        {
            StartupDebugLog.Info("ShellPage", "Title bar elements were unavailable on load.");
            return;
        }

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.AppTitlebar = AppTitleBarText as UIElement;
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
        TitleBarHelper.UpdateTitleBar(App.MainWindow, RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        Unloaded += ShellPage_Unloaded;
    }

    private void OnStartupShellStateChanged(object? sender, EventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            ApplyStartupShellState();
        });
    }

    private void ApplyStartupShellState()
    {
        NavigationViewControl.IsPaneVisible = _startupShellStateService.IsNavigationVisible;
        NavigationViewControl.IsSettingsVisible = _startupShellStateService.IsNavigationVisible;
    }

    private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
    {
        NavigationFrame.Navigated -= NavigationFrame_Navigated;
        _startupShellStateService.StateChanged -= OnStartupShellStateChanged;
        Unloaded -= ShellPage_Unloaded;
    }

    private void NavigationFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        SyncMyRequestsVisibility();
    }

    private void SyncMyRequestsVisibility()
    {
        if (NavigationFrame.GetPageViewModel() is WaitlistViewViewModel waitlistViewModel)
        {
            // The My Requests header filter applies only while the Waitlist (list) view is
            // active; keep its toggle in sync with the active view model's filter state.
            ViewModel.IsMyRequestsVisible = true;
            ViewModel.ShowMyRequestsOnly = waitlistViewModel.ShowMyRequestsOnly;
        }
        else
        {
            ViewModel.IsMyRequestsVisible = false;
            TitleBarSearchBox.Text = string.Empty;
            TitleBarSearchBox.ItemsSource = null;
        }
    }

    private void MyRequestsToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (NavigationFrame.GetPageViewModel() is WaitlistViewViewModel waitlistViewModel
            && sender is ToggleSwitch toggle)
        {
            waitlistViewModel.ShowMyRequestsOnly = toggle.IsOn;
        }
    }

    private void TitleBarSearchBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        if (NavigationFrame.GetPageViewModel() is not WaitlistViewViewModel viewModel)
        {
            sender.ItemsSource = null;
            return;
        }

        viewModel.UpdateSearchSuggestions(sender.Text);
        sender.ItemsSource = viewModel.SearchSuggestions;
    }

    private void TitleBarSearchBox_SuggestionChosen(
        AutoSuggestBox sender,
        AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SampleOrder order)
        {
            sender.Text = order.Title;
        }
    }

    private void TitleBarSearchBox_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (NavigationFrame.GetPageViewModel() is WaitlistViewViewModel viewModel)
        {
            viewModel.SubmitSearch(args.QueryText, args.ChosenSuggestion as SampleOrder);
        }
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }
}
