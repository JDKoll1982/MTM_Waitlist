using MTM_Waitlist.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MTM_Waitlist.Views;

public sealed partial class SplashView : UserControl
{
    public SplashViewModel ViewModel { get; }

    public SplashView()
    {
        ViewModel = App.GetService<SplashViewModel>();
        InitializeComponent();
        ViewModel.LoggingDestinationPromptRequestedAsync = PromptForLoggingDestinationAsync;
    }

    private async Task<string?> PromptForLoggingDestinationAsync()
    {
        await EnsureLoadedAsync();

        if (XamlRoot is null)
        {
            return null;
        }

        var destinationBox = new TextBox
        {
            PlaceholderText = "Choose or type a folder path",
            MinWidth = 340,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var browseButton = new Button
        {
            Content = "...",
            MinWidth = 44
        };
        ToolTipService.SetToolTip(browseButton, "Browse for folder");

        browseButton.Click += async (_, _) =>
        {
            browseButton.IsEnabled = false;
            try
            {
                var selectedPath = await BrowseForDestinationAsync();
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    destinationBox.Text = selectedPath;
                }
            }
            finally
            {
                browseButton.IsEnabled = true;
            }
        };

        var destinationRow = new Grid
        {
            ColumnSpacing = 8,
            MinWidth = 420
        };
        destinationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        destinationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        destinationRow.Children.Add(destinationBox);
        destinationRow.Children.Add(browseButton);
        Grid.SetColumn(destinationBox, 0);
        Grid.SetColumn(browseButton, 1);

        var dialogContent = new StackPanel
        {
            Spacing = 12
        };

        dialogContent.Children.Add(new TextBlock
        {
            Text = "Before we continue, choose where startup logs should be saved.",
            TextWrapping = TextWrapping.Wrap
        });
        dialogContent.Children.Add(destinationRow);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Choose a log folder",
            Content = dialogContent,
            PrimaryButtonText = "Save and continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        destinationBox.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(destinationBox.Text);

        try
        {
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? destinationBox.Text?.Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (XamlRoot is not null)
        {
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RoutedEventHandler? loadedHandler = null;
        loadedHandler = (_, _) =>
        {
            Loaded -= loadedHandler;
            tcs.TrySetResult();
        };

        Loaded += loadedHandler;
        await tcs.Task;
    }

    private static async Task<string?> BrowseForDestinationAsync()
    {
        var hwnd = NativeMethods.GetActiveWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var folderPicker = new FolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private static partial class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();
    }
}
