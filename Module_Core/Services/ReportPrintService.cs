using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using Windows.Foundation;
using Windows.Graphics.Printing;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class ReportPrintService : IReportPrintService
{
    private static readonly SolidColorBrush TitleBrush = new(ColorHelper.FromArgb(0xFF, 0x1F, 0x38, 0x64));
    private static readonly SolidColorBrush SubtitleBrush = new(ColorHelper.FromArgb(0xFF, 0x59, 0x59, 0x59));
    private static readonly SolidColorBrush MutedBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x8A, 0x8A));
    private static readonly SolidColorBrush LabelBrush = new(ColorHelper.FromArgb(0xFF, 0x40, 0x40, 0x40));
    private static readonly SolidColorBrush DividerBrush = new(ColorHelper.FromArgb(0xFF, 0xD0, 0xD0, 0xD0));
    private static readonly SolidColorBrush ValueBrush = new(Colors.Black);

    private PrintManager? _printManager;
    private PrintDocument? _printDocument;
    private IPrintDocumentSource? _printDocumentSource;
    private PrintableReport? _currentReport;
    private readonly List<UIElement> _pages = new();

    public bool IsRegistered { get; private set; }

    public void Register(Window window)
    {
        if (IsRegistered)
        {
            return;
        }

        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _printManager = PrintManagerInterop.GetForWindow(hwnd);
            _printManager.PrintTaskRequested += OnPrintTaskRequested;

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            _printDocument.Paginate += OnPaginate;
            _printDocument.GetPreviewPage += OnGetPreviewPage;
            _printDocument.AddPages += OnAddPages;

            IsRegistered = true;
        }
        catch
        {
            IsRegistered = false;
        }
    }

    public void Unregister()
    {
        if (_printDocument is not null)
        {
            _printDocument.Paginate -= OnPaginate;
            _printDocument.GetPreviewPage -= OnGetPreviewPage;
            _printDocument.AddPages -= OnAddPages;
        }

        if (_printManager is not null)
        {
            _printManager.PrintTaskRequested -= OnPrintTaskRequested;
        }

        _printDocument = null;
        _printManager = null;
        _printDocumentSource = null;
        _currentReport = null;
        _pages.Clear();
        IsRegistered = false;
    }

    public async Task<bool> PrintAsync(Window window, PrintableReport report)
    {
        if (!IsRegistered)
        {
            Register(window);
        }

        if (!IsRegistered || !PrintManager.IsSupported())
        {
            return false;
        }

        _currentReport = report;
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            await PrintManagerInterop.ShowPrintUIForWindowAsync(hwnd);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        args.Request.CreatePrintTask("Report", OnPrintTaskSourceRequested);
    }

    private void OnPrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
    {
        if (_printDocumentSource is not null)
        {
            args.SetSource(_printDocumentSource);
        }
    }

    private void OnPaginate(object sender, PaginateEventArgs e)
    {
        _pages.Clear();
        var pageDescription = e.PrintTaskOptions.GetPageDescription(0);
        BuildPages(pageDescription.PageSize.Width, pageDescription.PageSize.Height);
        _printDocument?.SetPreviewPageCount(_pages.Count, PreviewPageCountType.Final);
    }

    private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        if (_printDocument is not null && e.PageNumber >= 1 && e.PageNumber <= _pages.Count)
        {
            _printDocument.SetPreviewPage(e.PageNumber, _pages[e.PageNumber - 1]);
        }
    }

    private void OnAddPages(object sender, AddPagesEventArgs e)
    {
        if (_printDocument is null)
        {
            return;
        }

        foreach (var page in _pages)
        {
            _printDocument.AddPage(page);
        }

        _printDocument.AddPagesComplete();
    }

    private void BuildPages(double pageWidth, double pageHeight)
    {
        const double margin = 48;
        var contentWidth = Math.Max(0, pageWidth - margin * 2);
        var contentHeight = Math.Max(0, pageHeight - margin * 2);

        var elements = BuildReportElements(_currentReport);

        _pages.Clear();
        var currentPanel = new StackPanel { Width = contentWidth };
        var currentHeight = 0.0;

        foreach (var element in elements)
        {
            element.Measure(new Size(contentWidth, double.PositiveInfinity));
            var elementHeight = element.DesiredSize.Height + element.Margin.Bottom;

            if (currentPanel.Children.Count > 0 && currentHeight + elementHeight > contentHeight)
            {
                _pages.Add(WrapPage(currentPanel, pageWidth, pageHeight, margin));
                currentPanel = new StackPanel { Width = contentWidth };
                currentHeight = 0.0;
            }

            currentPanel.Children.Add(element);
            currentHeight += elementHeight;
        }

        if (currentPanel.Children.Count > 0)
        {
            _pages.Add(WrapPage(currentPanel, pageWidth, pageHeight, margin));
        }

        if (_pages.Count == 0)
        {
            _pages.Add(WrapPage(new StackPanel { Width = contentWidth }, pageWidth, pageHeight, margin));
        }
    }

    private static Grid WrapPage(StackPanel panel, double pageWidth, double pageHeight, double margin)
    {
        var grid = new Grid { Width = pageWidth, Height = pageHeight };
        panel.Margin = new Thickness(margin);
        grid.Children.Add(panel);
        return grid;
    }

    private static List<FrameworkElement> BuildReportElements(PrintableReport? report)
    {
        var elements = new List<FrameworkElement>();

        if (report is null)
        {
            AddText(elements, "Report", 24, bold: true, TitleBrush, bottom: 8);
            AddText(elements, "No report content.", 12, bold: false, MutedBrush, bottom: 0);
            return elements;
        }

        // Header
        AddText(elements, report.Title, 24, bold: true, TitleBrush, bottom: 6);

        if (!string.IsNullOrWhiteSpace(report.Subtitle))
        {
            AddText(elements, report.Subtitle, 14, bold: false, SubtitleBrush, bottom: 6);
        }

        AddText(elements, $"Generated {DateTime.Now:yyyy-MM-dd HH:mm}", 11, bold: false, MutedBrush, bottom: 12);
        elements.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 0, 0, 10) });

        // Sections
        foreach (var section in report.Sections)
        {
            AddText(elements, section.Title.ToUpperInvariant(), 12, bold: true, TitleBrush, bottom: 4);
            elements.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 0, 0, 6) });

            foreach (var field in section.Fields)
            {
                AddField(elements, field.Label, field.Value);
            }

            foreach (var line in section.Lines)
            {
                AddFileLine(elements, line);
            }

            elements.Add(new Border { Height = 10 });
        }

        // Footer
        foreach (var footer in report.FooterLines)
        {
            AddText(elements, footer, 11, bold: false, MutedBrush, bottom: 2);
        }

        return elements;
    }

    private static void AddText(
        List<FrameworkElement> elements,
        string text,
        double fontSize,
        bool bold,
        SolidColorBrush foreground,
        double bottom)
    {
        elements.Add(new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = foreground,
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 0, 0, bottom),
        });
    }

    private static void AddField(List<FrameworkElement> elements, string label, string? value)
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 0, 0, 3),
        };
        textBlock.Inlines.Add(new Run
        {
            Text = label,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = LabelBrush,
        });
        textBlock.Inlines.Add(new Run
        {
            Text = $": {value}",
            FontSize = 12,
            Foreground = ValueBrush,
        });
        elements.Add(textBlock);
    }

    private static void AddFileLine(List<FrameworkElement> elements, string text)
    {
        var textBlock = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 0, 0, 2),
        };
        textBlock.Inlines.Add(new Run
        {
            Text = "\u2022  ",
            FontSize = 12,
            Foreground = TitleBrush,
        });
        textBlock.Inlines.Add(new Run
        {
            Text = text,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            Foreground = ValueBrush,
        });
        elements.Add(textBlock);
    }
}
