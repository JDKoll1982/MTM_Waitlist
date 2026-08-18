using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;
using Windows.Storage.Pickers;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// Shared body for the three image-override dialogs.
/// </summary>
public sealed partial class ImageOverrideEditorControl : UserControl
{
    private ImageOverrideDialogViewModel? _viewModel;

    public ImageOverrideEditorControl()
    {
        InitializeComponent();
    }

    public void Attach(ImageOverrideDialogViewModel viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        GroupsItemsControl.ItemsSource = _viewModel.Groups;
        SearchTextBox.PlaceholderText = _viewModel.SupportsGrouping ? "Search by name or group" : "Search by name";

        RefreshState();
    }

    public void Detach()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel = null;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshState();

    private void RefreshState()
    {
        if (_viewModel is null)
        {
            return;
        }

        LoadingRing.IsActive = _viewModel.IsLoading;
        LoadingRing.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        RowsScrollViewer.Visibility = _viewModel.IsLoading ? Visibility.Collapsed : Visibility.Visible;

        ErrorInfoBar.Message = _viewModel.ErrorMessage;
        ErrorInfoBar.IsOpen = _viewModel.HasError;

        StatusInfoBar.Message = _viewModel.StatusMessage;
        StatusInfoBar.IsOpen = _viewModel.HasStatus;

        EmptyTextBlock.Visibility = _viewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        ResetAllButton.IsEnabled = _viewModel.CanSave && _viewModel.HasRows;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SearchText = SearchTextBox.Text;
        }
    }

    private void CustomOnlyToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ShowOnlyCustomImages = CustomOnlyToggle.IsOn;
        }
    }

    private async void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Reset all overrides?",
            Content = "Every custom image in this list will be cleared. Nothing is written until you choose Save.",
            PrimaryButtonText = "Reset all",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await _viewModel.ResetAllAsync();
        }
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not Button { Tag: ImageOverrideRow row })
        {
            return;
        }

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        // WinUI 3 pickers are unowned until associated with the app window.
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            _viewModel.SetRowPath(row, file.Path);
        }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not Button { Tag: ImageOverrideRow row })
        {
            return;
        }

        await _viewModel.ResetRowCommand.ExecuteAsync(row);
    }
}
