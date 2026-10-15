using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.ViewModels;

using WinUIEx;

namespace MTM_Waitlist.Module_Shared.Views;

/// <summary>
/// The developer control inspector's surface: one control's details, in a window of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a window and not a page.</b> The inspector is opened from a control that is being hovered, and a hovered
/// control is very often inside a modal dialog. A <c>ContentDialog</c> owns its <c>XamlRoot</c> and blocks it, so a
/// page navigated to behind that dialog can never be seen — the shortcut looked as though it did nothing. This
/// window has no <c>XamlRoot</c> to be blocked by, so it opens over the dialog that raised it, over the shell, or
/// over any other window the app shows.
/// </para>
/// <para>
/// <b>It is not a stage.</b> The one window the launch opens and hands to the shell
/// (<c>contracts/window-stage-contract.md</c> §1) is the application's stage host, and this is not a stage of it:
/// nothing selects it, it hosts none of the five stages, and it is reachable only through the developer shortcut.
/// It is closed rather than navigated away from, so it never takes part in the shell's back stack either.
/// </para>
/// </remarks>
public sealed partial class ControlInspectorDetailWindow : WindowEx
{
    private readonly IReportPrintService _reportPrintService;

    public ControlInspectorDetailViewModel ViewModel { get; }

    public ControlInspectorDetailWindow()
    {
        ViewModel = App.GetService<ControlInspectorDetailViewModel>();
        _reportPrintService = App.GetService<IReportPrintService>();

        InitializeComponent();

        // Sized like a reading surface rather than a dialog: the detail carries several tables and a file list.
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 860));
        WindowStartupPlacement.CentreOnScreen(this, "ControlInspector");
    }

    /// <summary>
    /// Shows one control's details, reusing this window when it is already open.
    /// </summary>
    /// <param name="detail">The details the shortcut built from the hovered control.</param>
    public void ShowDetail(ControlInspectorDetail? detail)
    {
        ViewModel.Load(detail);

        Title = string.IsNullOrWhiteSpace(detail?.Title)
            ? "Control details"
            : $"Control details — {detail!.Title}";

        Activate();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnContentLoaded(object sender, RoutedEventArgs e)
    {
        // The report prints this window, not the shell: the person asked for the control's details, and the details
        // are what is on screen here.
        _reportPrintService.Register(this);
        PrintButton.IsEnabled = _reportPrintService.IsRegistered;
    }

    private void OnContentUnloaded(object sender, RoutedEventArgs e) => _reportPrintService.Unregister();

    private async void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (!_reportPrintService.IsRegistered)
        {
            await ShowPrintMessageAsync(
                "Printing unavailable",
                "Printing could not be initialized for this window.");
            return;
        }

        var report = BuildReport(ViewModel.Detail);
        var printed = await _reportPrintService.PrintAsync(this, report);
        if (!printed)
        {
            await ShowPrintMessageAsync(
                "Printing unavailable",
                "Printing is not supported on this device or can't proceed right now.");
        }
    }

    private static PrintableReport BuildReport(ControlInspectorDetail? detail)
    {
        var sections = new List<PrintableReportSection>();
        foreach (var section in detail?.Sections ?? Array.Empty<ControlInspectorSection>())
        {
            if (string.Equals(section.Title, "Associated files", StringComparison.OrdinalIgnoreCase))
            {
                sections.Add(new PrintableReportSection
                {
                    Title = section.Title,
                    Lines = section.Fields
                        .Select(static field => field.Value)
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .ToArray(),
                });
                continue;
            }

            sections.Add(new PrintableReportSection
            {
                Title = section.Title,
                Fields = section.Fields
                    .Select(static field => new PrintableReportField
                    {
                        Label = field.Label,
                        Value = field.Value,
                    })
                    .ToArray(),
            });
        }

        return new PrintableReport
        {
            Title = detail?.Title ?? "Control details",
            Subtitle = detail?.Summary ?? string.Empty,
            Sections = sections,
            FooterLines = new[]
            {
                "Generated by MTM Waitlist Control Inspector",
                "Opened with Ctrl+Alt+Right-Click on a tracked control.",
            },
        };
    }

    private async Task ShowPrintMessageAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "OK",

            // This window's own root, because the dialog belongs to the window that raised it.
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
