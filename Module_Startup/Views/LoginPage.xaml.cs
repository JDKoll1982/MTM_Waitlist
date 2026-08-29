using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

public sealed partial class LoginPage : Page
{
    private bool _isInitialized;
    private bool _isGateDialogOpen;

    public LoginViewModel ViewModel
    {
        get;
    }

    public LoginPage()
    {
        ViewModel = App.GetService<LoginViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.InitializeAsync();
        PasswordBox.Password = ViewModel.Password;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.ComputerGateState))
        {
            return;
        }

        await ShowComputerGateDialogAsync();
    }

    private async Task ShowComputerGateDialogAsync()
    {
        if (_isGateDialogOpen)
        {
            return;
        }

        _isGateDialogOpen = true;
        try
        {
            switch (ViewModel.ComputerGateState)
            {
                case ComputerGateStatus.Missing:
                case ComputerGateStatus.RenamedMachine:
                    await ShowAddComputerDialogAsync();
                    break;

                case ComputerGateStatus.DatabaseUnavailable:
                    await ShowDatabaseRetryDialogAsync();
                    break;
            }
        }
        finally
        {
            _isGateDialogOpen = false;
        }
    }

    private async Task ShowAddComputerDialogAsync()
    {
        while (true)
        {
            var isRenamed = ViewModel.ComputerGateState == ComputerGateStatus.RenamedMachine;

            var detectedText = new TextBlock
            {
                Text = $"Computer: {ViewModel.DetectedComputerName}\nMAC: {ViewModel.DetectedMacAddress}",
                Opacity = 0.7,
                Margin = new Thickness(0, 0, 0, 8),
            };

            var errorText = new TextBlock
            {
                Text = ViewModel.ComputerGateError,
                TextWrapping = TextWrapping.Wrap,
                Visibility = string.IsNullOrWhiteSpace(ViewModel.ComputerGateError)
                    ? Visibility.Collapsed
                    : Visibility.Visible,
            };

            var nameInput = new TextBox
            {
                Header = "Display name (required)",
                PlaceholderText = "e.g. John's Computer",
                Text = ViewModel.ComputerDisplayName,
                MinWidth = 320,
            };

            var descriptionInput = new TextBox
            {
                Header = "Description (optional)",
                PlaceholderText = "Optional notes",
                Text = ViewModel.ComputerDescription,
                MinWidth = 320,
            };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(detectedText);
            panel.Children.Add(nameInput);
            panel.Children.Add(descriptionInput);
            panel.Children.Add(errorText);

            var dialog = new ContentDialog
            {
                Title = isRenamed ? "Confirm computer" : "Register this computer",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                // Hard gate: cancel keeps the app blocked until a computer is saved.
                ViewModel.ComputerGateError = "This computer must be registered before you can continue.";
                continue;
            }

            ViewModel.ComputerDisplayName = nameInput.Text;
            ViewModel.ComputerDescription = descriptionInput.Text;

            var saved = await ViewModel.CompleteComputerGateAsync();
            if (saved)
            {
                return;
            }
        }
    }

    private async Task ShowDatabaseRetryDialogAsync()
    {
        while (ViewModel.ComputerGateState == ComputerGateStatus.DatabaseUnavailable)
        {
            var hintText = new TextBlock
            {
                Text = ViewModel.ComputerGateHint,
                TextWrapping = TextWrapping.Wrap,
            };

            var dialog = new ContentDialog
            {
                Title = "Database unavailable",
                Content = hintText,
                PrimaryButtonText = "Retry",
                CloseButtonText = "Exit",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                App.Current.Exit();
                return;
            }

            var status = await ViewModel.RetryComputerGateAsync();
            if (status == ComputerGateStatus.DatabaseUnavailable)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                continue;
            }

            if (status is ComputerGateStatus.Missing or ComputerGateStatus.RenamedMachine)
            {
                await ShowAddComputerDialogAsync();
                return;
            }

            return;
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.Password = passwordBox.Password;
        }
    }

    private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.NewPassword = passwordBox.Password;
        }
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.ConfirmPassword = passwordBox.Password;
        }
    }
}
