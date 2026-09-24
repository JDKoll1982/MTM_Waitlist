using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Shared.Controls;

/// <summary>
/// A content picture that can be enlarged by clicking it: one control, so every surface that draws a picture
/// answers the click the same way and no page carries an image-viewer overlay of its own (FR-028 to FR-032).
/// </summary>
/// <remarks>
/// <para>
/// The enlarged view is a full-size dialog rather than a page overlay. A page overlay has to be declared by every
/// page that wants one, which is exactly the duplication this control removes; a dialog is shown from here and
/// disappears with the control's own screen.
/// </para>
/// <para>
/// Dismissing it: the dialog's own close button and <c>Esc</c>, both of which the dialog framework supplies. A
/// click on the dimmed area behind it is not a dismissal — a dialog does not light-dismiss — so nothing in this
/// control pretends otherwise.
/// </para>
/// <para>
/// Whether the click does anything is a per-person preference (on by default). It is read from the host's
/// services when the control is loaded; when no host is present (a designer, a unit test, a control hosted
/// outside the app) the documented default is used, so the picture is still enlargeable rather than dead.
/// </para>
/// </remarks>
public sealed partial class ClickToEnlargeImageView : UserControl
{
    /// <summary>The picture to draw, already resolved through the application's one picture rule.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(ImageSource),
        typeof(ClickToEnlargeImageView),
        new PropertyMetadata(null));

    /// <summary>How the picture fills its frame. The frame decides the size; this decides the fit.</summary>
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(Stretch),
        typeof(ClickToEnlargeImageView),
        new PropertyMetadata(Stretch.Uniform));

    private IPictureEnlargePreference? _enlargePreference;

    public ClickToEnlargeImageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// The preference, resolved once from the host's services. A missing host is not an error: the control then
    /// behaves as the shipped default (enlarging on), which is also what a reader with an unreachable store gets.
    /// </summary>
    private IPictureEnlargePreference? Preference
    {
        get
        {
            if (_enlargePreference is null)
            {
                try
                {
                    _enlargePreference = App.GetService<IPictureEnlargePreference>();
                }
                catch (Exception)
                {
                    _enlargePreference = null;
                }
            }

            return _enlargePreference;
        }
    }

    private bool IsEnlargingOffered => Source is not null && (Preference?.IsEnabled ?? true);

    private void OnLoaded(object sender, RoutedEventArgs e) => ApplyAffordance();

    /// <summary>
    /// Offers the hint only while a click would do something. With the feature switched off the picture keeps its
    /// name and loses the tooltip, so nothing announces an action that will not happen (FR-031).
    /// </summary>
    private void ApplyAffordance() =>
        ToolTipService.SetToolTip(this, IsEnlargingOffered ? "Shared_ClickToEnlarge.Tooltip".GetLocalized() : null);

    private async void OnThumbnailTapped(object sender, TappedRoutedEventArgs e)
    {
        // The preference is re-read here as well as on load: the store answers after start-up, and a reader who
        // has just switched the feature off should not get one more enlarging out of the open screen.
        ApplyAffordance();

        if (!IsEnlargingOffered || XamlRoot is null)
        {
            return;
        }

        await ShowEnlargedAsync();
    }

    private async Task ShowEnlargedAsync()
    {
        var enlarged = new Image
        {
            Source = Source,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Bounded by the window rather than by the picture: the dialog measures to its content, so a picture wider
        // than the screen would otherwise ask for a dialog wider than the screen. Verified in the running app
        // 2026-09-24 — the dialog held a natural-size picture and a Close button, with the page dimmed behind it.
        if (XamlRoot is { } root)
        {
            enlarged.MaxWidth = Math.Max(240, root.Size.Width - 96);
            enlarged.MaxHeight = Math.Max(240, root.Size.Height - 160);
        }

        AutomationProperties.SetName(enlarged, PictureDescription());

        var viewer = new ContentDialog
        {
            XamlRoot = XamlRoot,
            FullSizeDesired = true,
            CloseButtonText = "Shared_ClickToEnlarge.CloseButton".GetLocalized(),
            Content = enlarged,
        };

        AutomationProperties.SetAutomationId(viewer, "ClickToEnlargeImageView_Viewer");
        AutomationProperties.SetName(viewer, PictureDescription());

        try
        {
            await viewer.ShowAsync();
        }
        catch (Exception)
        {
            // Only one dialog may be open at a time. Being unable to enlarge is not a failure the reader needs to
            // hear about: the picture is still on the screen, at thumbnail size.
        }
    }

    /// <summary>The consumer's name for the picture, or this control's own wording when it was given none.</summary>
    private string PictureDescription()
    {
        var name = AutomationProperties.GetName(this);

        return string.IsNullOrWhiteSpace(name)
            ? "Shared_ClickToEnlarge.ViewerName".GetLocalized()
            : name;
    }
}
