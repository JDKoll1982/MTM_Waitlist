using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

using SaveFilePicker = Microsoft.Windows.Storage.Pickers.FileSavePicker;
using OpenFilePicker = Windows.Storage.Pickers.FileOpenPicker;
using PickerLocationId = Windows.Storage.Pickers.PickerLocationId;
using PickerViewMode = Windows.Storage.Pickers.PickerViewMode;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The part-picture screen's code-behind. The two pickers live here because picking a file is a platform
/// capability that needs the window, and everything else lives in the view model so it can be driven without one.
/// </summary>
public sealed partial class PartPictureManagerPage : Page
{
    public PartPictureManagerViewModel ViewModel { get; }

    public PartPictureManagerPage()
    {
        StartupDebugLog.Info("PartPictureManagerPage", "Constructor started.");
        ViewModel = App.GetService<PartPictureManagerViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("PartPictureManagerPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // The list is what the screen exists to work through, so it is read as the page opens rather than waiting
        // for a click.
        _ = ViewModel.LoadMissingPartsAsync();
    }

    /// <summary>Chooses a picture and stores it for the part in front of the person.</summary>
    /// <remarks>
    /// One action rather than two: the person picks the file and the picture is saved, so "set" and "replace" are
    /// the same gesture and the button's own label says which one is about to happen.
    /// </remarks>
    private async void ChoosePicture_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFilePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        // WinUI 3 pickers are unowned until associated with the app window.
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ViewModel.ApplyPictureAsync(file.Path);
        }
    }

    /// <summary>Writes the missing-picture list to the file the person chose.</summary>
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var savePicker = new SaveFilePicker(App.MainWindow.AppWindow.Id)
        {
            SuggestedFileName = ViewModel.ExportSuggestedFileName,
            DefaultFileExtension = ".csv",
            FileTypeChoices = { { "CSV", new List<string> { ".csv" } } },
        };

        var result = await savePicker.PickSaveFileAsync();
        if (result is null)
        {
            // A cancelled picker is not a failure: the person changed their mind about where it goes.
            return;
        }

        try
        {
            File.WriteAllText(result.Path, ViewModel.ExportCsv);
            ViewModel.NoteExportWritten(ViewModel.MissingParts.Count);
        }
        catch (IOException ex)
        {
            StartupDebugLog.Error("PartPictureManagerPage", ex, "The missing-picture list could not be written.");
            ViewModel.ReportExportFailure(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            StartupDebugLog.Error("PartPictureManagerPage", ex, "The missing-picture list could not be written.");
            ViewModel.ReportExportFailure(ex.Message);
        }
    }
}
