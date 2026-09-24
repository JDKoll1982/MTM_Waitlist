using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

using Windows.Foundation;
using Windows.System;

namespace MTM_Waitlist.Module_Shared.Controls;

/// <summary>
/// A content picture that can be enlarged by clicking it: one control, so every surface that draws a picture
/// answers the click the same way and no page carries an image-viewer overlay of its own (FR-028 to FR-032).
/// </summary>
/// <remarks>
/// <para>
/// The enlarged view is the control's own <c>Popup</c>, sized to the page the picture was clicked on. It covers
/// that page, the page stays visible through the backdrop's semi-transparent fill, and a click anywhere on the
/// backdrop closes it. A dialog was rejected here: a dialog draws chrome of its own, is measured against its
/// content rather than the page, and cannot be dismissed by clicking around it, so it can do none of those three
/// things.
/// </para>
/// <para>
/// <b>The gesture follows the host.</b> Where the picture stands on its own, a plain click enlarges it. Where the
/// picture is part of something that acts on a click — inside a button, or inside a list row that raises
/// <c>ItemClick</c> or selects on a click, such as a waitlist card or a card in the shell's building flyout — a
/// plain click belongs to that host, and <b>Shift+click</b> is what enlarges the picture. The tooltip says which
/// gesture applies, so nothing announces an action a plain click would not perform. The nearest host decides, and
/// it is worked out from the picture's own place in the tree rather than declared per surface, so a card list that
/// does not act on a click keeps the click and a card moved into one that does cannot quietly take it away.
/// </para>
/// <para>
/// Dismissing it: the backdrop around the picture, the X in the top-right corner, and <c>Esc</c>. A click on the
/// picture itself is not a dismissal, so a reader looking closely at it does not lose it.
/// </para>
/// <para>
/// Whether a click does anything at all is a per-person preference (on by default). It is read from the host's
/// services when the control is loaded; when no host is present (a designer, a unit test, a control hosted
/// outside the app) the documented default is used, so the picture is still enlargeable rather than dead.
/// </para>
/// </remarks>
public sealed partial class ClickToEnlargeImageView : UserControl
{
    /// <summary>
    /// The edge kept free around the enlarged picture. The picture's own margin and the size cap both use it, so
    /// the backdrop stays clickable all the way round and the close button never sits on the picture.
    /// </summary>
    private const double PictureMargin = 64;

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
    private XamlRoot? _sizedRoot;

    public ClickToEnlargeImageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
    /// The view is closed when the control leaves the tree. These controls live inside virtualized lists, and the
    /// popup outlives the control's place in the tree, so it is closed here rather than left covering the page
    /// with nothing on the page left to close it.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e) => CloseEnlarged();

    /// <summary>
    /// Offers the hint only while a click would do something, and names the gesture that applies here. With the
    /// feature switched off the picture keeps its name and loses the tooltip, so nothing announces an action that
    /// will not happen (FR-031).
    /// </summary>
    private void ApplyAffordance()
    {
        ToolTipService.SetToolTip(
            this,
            IsEnlargingOffered
                ? (IsClickSpokenFor ? "Shared_ClickToEnlarge.ShiftTooltip" : "Shared_ClickToEnlarge.Tooltip").GetLocalized()
                : null);

        // The X carries the same wording as the board's close, so the affordance is announced rather than being an
        // unlabelled glyph.
        var closeWording = "Shared_ClickToEnlarge.CloseButton".GetLocalized();
        AutomationProperties.SetName(CloseButton, closeWording);
        ToolTipService.SetToolTip(CloseButton, closeWording);
    }

    /// <summary>
    /// Whether a plain click on this picture already belongs to something else: a picture inside a button, or
    /// inside a list row that acts on a click, cannot answer a plain click itself without the reader losing the
    /// action they were aiming at. Those pictures enlarge on Shift+click instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The host is read from the picture's ancestors rather than declared by each surface, and the **nearest** host
    /// decides: a button around the picture, or the first list it sits in. The walk stops at that list either way,
    /// so a card list that does not act on a click (the Settings dunnage lists, whose action is their own Hide and
    /// Show buttons) leaves the click to the picture even when the page around it is a list of some other kind.
    /// </para>
    /// <para>
    /// Getting this wrong is not obvious on screen, which is why it was driven in the running app: a picture that
    /// needs Shift looks exactly like one that does not, right up to the moment somebody clicks it.
    /// </para>
    /// </remarks>
    private bool IsClickSpokenFor
    {
        get
        {
            for (var ancestor = VisualTreeHelper.GetParent(this); ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor))
            {
                switch (ancestor)
                {
                    case ButtonBase:
                        return true;

                    // The first list is the one this picture's row belongs to, whatever sits around it.
                    case ListViewBase list:
                        return list.IsItemClickEnabled || list.SelectionMode != ListViewSelectionMode.None;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// The one gesture that has to be caught before the host sees it. A Shift+click on a picture whose plain click
    /// is spoken for is taken here, on the press rather than on the tap, so the button or the list row around the
    /// picture never acts on it; every other click is left to the host, or to <see cref="OnThumbnailTapped" />.
    /// </summary>
    private void OnThumbnailPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ApplyAffordance();

        if (!IsEnlargingOffered || !IsClickSpokenFor || !IsShiftHeld(e) || XamlRoot is null)
        {
            return;
        }

        ShowEnlarged();
        e.Handled = true;
    }

    private void OnThumbnailTapped(object sender, TappedRoutedEventArgs e)
    {
        // The preference is re-read here as well as on load: the store answers after start-up, and a reader who
        // has just switched the feature off should not get one more enlarging out of the open screen.
        ApplyAffordance();

        if (!IsEnlargingOffered || IsClickSpokenFor || XamlRoot is null)
        {
            return;
        }

        ShowEnlarged();
    }

    /// <summary>
    /// Whether Shift was held for this pointer event. The modifiers come from the event itself rather than from the
    /// keyboard's current state: verified in the running app (2026-09-24) that the state read at handling time does
    /// not see the Shift of the very click being handled, so the gesture would simply never fire.
    /// </summary>
    private static bool IsShiftHeld(PointerRoutedEventArgs e) =>
        (e.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;

    /// <summary>Opens the enlarged view over the page the picture was clicked on.</summary>
    private void ShowEnlarged()
    {
        if (EnlargedView.IsOpen)
        {
            return;
        }

        SizeToPage();

        AutomationProperties.SetName(EnlargedBackdrop, PictureDescription());

        EnlargedView.IsOpen = true;

        // Focus goes to the close button, which is also what puts the keyboard inside the popup: a Popup does not
        // fire key events of its own, so Esc is handled on its child and only reaches that child while focus is
        // in there.
        CloseButton.Focus(FocusState.Programmatic);
    }

    private void CloseEnlarged()
    {
        if (!EnlargedView.IsOpen)
        {
            return;
        }

        EnlargedView.IsOpen = false;

        if (_sizedRoot is not null)
        {
            _sizedRoot.Changed -= OnPageSizeChanged;
            _sizedRoot = null;
        }
    }

    /// <summary>
    /// Sizes the view to the page it covers and puts it at the page's own origin. A popup measures to its child
    /// and is placed where it sits in the layout, so the backdrop is given the page's size and offset back to the
    /// page's top-left: the page stays visible through the backdrop's semi-transparent fill, every point on it is a
    /// click target that closes the view, and the picture is capped to the same rectangle — fitted to the page
    /// where it is bigger than the page, drawn at its own size where it is smaller.
    /// </summary>
    private void SizeToPage()
    {
        if (XamlRoot is not { } page)
        {
            return;
        }

        // Rounded up: a fractional page size must not leave a sliver of the page uncovered along an edge, where a
        // click would reach the page behind the view.
        var width = Math.Ceiling(page.Size.Width);
        var height = Math.Ceiling(page.Size.Height);

        EnlargedBackdrop.Width = width;
        EnlargedBackdrop.Height = height;

        EnlargedPicture.MaxWidth = Math.Max(1, width - (2 * PictureMargin));
        EnlargedPicture.MaxHeight = Math.Max(1, height - (2 * PictureMargin));

        // A popup is placed where it sits in the layout — beside the picture that opened it — so it is offset back
        // to the page's origin. Without this the backdrop would be drawn next to the thumbnail rather than over
        // the page, and only part of it would be inside the window.
        if (EnlargedView.Parent is UIElement host && page.Content is UIElement content)
        {
            var origin = host.TransformToVisual(content).TransformPoint(new Point(0, 0));
            EnlargedView.HorizontalOffset = -origin.X;
            EnlargedView.VerticalOffset = -origin.Y;
        }

        if (_sizedRoot is null)
        {
            // Maximizing or resizing the window while the view is open would otherwise leave the backdrop behind
            // the page's new edge, with the area beyond it neither dimmed nor clickable.
            _sizedRoot = page;
            _sizedRoot.Changed += OnPageSizeChanged;
        }
    }

    private void OnPageSizeChanged(XamlRoot sender, XamlRootChangedEventArgs args) => SizeToPage();

    private void OnEnlargedBackdropTapped(object sender, TappedRoutedEventArgs e) => CloseEnlarged();

    /// <summary>A click on the picture is not a dismissal; the backdrop around it is.</summary>
    private void OnEnlargedPictureTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

    private void OnCloseClicked(object sender, RoutedEventArgs e) => CloseEnlarged();

    private void OnEnlargedBackdropKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        CloseEnlarged();
        e.Handled = true;
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
