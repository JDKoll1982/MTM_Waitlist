using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class RequestSubtypeImagesDialog : ContentDialog
{
    private readonly RequestSubtypeImagesDialogViewModel _viewModel;

    public RequestSubtypeImagesDialog(RequestSubtypeImagesDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Editor.Attach(_viewModel);

        Opened += OnOpened;
        PrimaryButtonClick += OnPrimaryButtonClick;
        CloseButtonClick += OnCloseButtonClick;
        Closed += OnClosed;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await _viewModel.LoadAsync();
        IsPrimaryButtonEnabled = _viewModel.CanSave;
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            args.Cancel = !await _viewModel.SaveAsync();
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
        _viewModel.CancelEdits();

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args) => Editor.Detach();
}
