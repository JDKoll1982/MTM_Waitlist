using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using MTM_Waitlist.Module_DevTools.ViewModels;

using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MTM_Waitlist.Module_DevTools.Views;

public sealed partial class RequestTypeBuilderPage : Page
{
    public RequestTypeBuilderViewModel ViewModel { get; }

    public RequestTypeBuilderPage()
    {
        ViewModel = App.GetService<RequestTypeBuilderViewModel>();
        InitializeComponent();
        ViewModel.PickImageFileAsync = BrowseForImageFileAsync;
    }

    private static async Task<string?> BrowseForImageFileAsync()
    {
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".ico");

        InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private ImageSource? ToImageSource(Uri? uri)
    {
        if (uri is null)
        {
            return null;
        }

        try
        {
            return new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }
}
