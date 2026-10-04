using System.Numerics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace MTM_Waitlist.Module_Shared.Controls;

/// <summary>
/// One line of text: drawn still when it fits, slid across its own field as a seamless ring when it does not, and
/// truncated with an ellipsis instead of moving when the system has animations switched off (FR-033 to FR-036).
/// </summary>
/// <remarks>
/// <para>
/// The scroll is a composition animation on a track's translation, so the work is done by the compositor rather
/// than the UI thread: there is no timer, no per-frame callback and nothing to collect when it stops. The
/// animation is created only for a line that actually overflows, and it is stopped when the control leaves the
/// tree or leaves the viewport — a long list therefore pays only for the lines a reader can see.
/// </para>
/// <para>
/// The repeat is seamless rather than a jump back: the track carries two copies of the value, the second one a gap
/// behind the first, and slides by exactly one copy plus that gap. When a pass ends the second copy stands where
/// the first one began, so the frame that closes a pass is the frame that opened it. Both copies are laid out at
/// their own full width and neither is trimmed, so every character is drawn on its way past.
/// </para>
/// <para>
/// This is deliberately dependency-free. There is no marquee text control in the Windows Community Toolkit to
/// adopt (its scrolling offering is <c>ScrollViewerExtensions</c> and <c>WrapPanel</c>), and a package that
/// reimplemented one would still be an animation over this same compositor primitive.
/// </para>
/// </remarks>
public sealed partial class AutoScrollTextView : UserControl
{
    /// <summary>Roughly the pace a person reads a scrolling line at, used to turn a distance into a duration.</summary>
    private const double ScrollPixelsPerSecond = 40;

    /// <summary>
    /// The room left between the end of the value and the copy that follows it, in pixels. Without it the loop would
    /// run the value straight onto itself, and the last character would read as if it began a longer word.
    /// </summary>
    private const double DefaultGap = 48;

    private static readonly TimeSpan s_shortestPass = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_longestPass = TimeSpan.FromSeconds(20);

    private static readonly Lazy<UISettings> s_uiSettings = new(() => new UISettings());

    /// <summary>The text to draw on one line.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(AutoScrollTextView),
        new PropertyMetadata(string.Empty, OnLineChanged));

    /// <summary>How the line sits in its field when it is drawn still.</summary>
    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
        nameof(TextAlignment),
        typeof(TextAlignment),
        typeof(AutoScrollTextView),
        new PropertyMetadata(TextAlignment.Left, OnLineChanged));

    /// <summary>The room left between the value and the copy of it the scroll follows with, so the loop is readable.</summary>
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap),
        typeof(double),
        typeof(AutoScrollTextView),
        new PropertyMetadata(DefaultGap, OnLineChanged));

    // What the last decision was made from, so a re-check that changes nothing does not re-measure the text or
    // restart the animation. These are only ever set by a decision, never cleared by stopping the scrolling: a
    // guard that a stop could clear is a guard that lets the next layout callback decide again.
    private string? _decidedForText;
    private double _decidedForWidth = double.NaN;
    private double _decidedForGap = double.NaN;

    // One decision at a time, taken after the current layout pass rather than inside it.
    private bool _decisionQueued;

    // The decision, and what is actually running: a line can be a scroller that is currently paused because it is
    // off screen, and it must start again when it comes back. The distance is one whole pass: the value, plus the
    // gap, which is what puts the second copy where the first one started.
    private bool _shouldScroll;
    private bool _isScrolling;
    private double _cycle;

    public AutoScrollTextView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        // A list recycles its containers, so this control can be handed a different value, or be scrolled out of
        // sight and back. Both cases are decided here rather than assumed once.
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ScheduleDecision();

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopScrolling();

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) => ScheduleDecision();

    private static void OnLineChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((AutoScrollTextView)sender).ScheduleDecision();

    /// <summary>
    /// A line that is not on screen is neither measured nor animated. Scrolling happens in the viewport or not at
    /// all, which is what keeps a list of these controls cheap; the decision itself is kept, so coming back into
    /// view restarts the scroll rather than re-measuring it.
    /// </summary>
    private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
    {
        if (args.EffectiveViewport.IsEmpty)
        {
            StopScrolling();
            return;
        }

        ApplyScroll();
        ScheduleDecision();
    }

    /// <summary>
    /// Asks for a decision, to be taken once the current layout pass has finished.
    /// </summary>
    /// <remarks>
    /// This indirection is load-bearing. Deciding changes the line's width, and this is reached from the very
    /// callbacks the layout pass raises — <c>SizeChanged</c> and <c>EffectiveViewportChanged</c>. Arranging an
    /// element from inside the arrange of its parent is how a screen reaches a layout cycle, and a cycle on the UI
    /// thread takes the process down without a managed exception: no event-log entry, no log line, just a window
    /// that disappears (observed 2026-09-24, opening the dunnage card lists).
    /// </remarks>
    private void ScheduleDecision()
    {
        if (_decisionQueued)
        {
            return;
        }

        _decisionQueued = true;

        if (DispatcherQueue?.TryEnqueue(OnDecisionDue) == true)
        {
            return;
        }

        // No dispatcher yet (the control is not in a tree). The next size change brings us back here.
        _decisionQueued = false;
    }

    private void OnDecisionDue()
    {
        _decisionQueued = false;
        Decide();
    }

    /// <summary>
    /// Decides from the room available and the room the text needs: a line that overflows moves, and everything
    /// else is drawn still. A line that overflows while the system has animations switched off is truncated.
    /// </summary>
    private void Decide()
    {
        var available = Viewport.ActualWidth;
        var height = Viewport.ActualHeight;

        if (!IsLoaded || available <= 0 || height <= 0)
        {
            // Not laid out yet. Laying out raises SizeChanged, which brings us back here with a real width.
            return;
        }

        if (string.Equals(_decidedForText, Text, StringComparison.Ordinal)
            && Math.Abs(_decidedForWidth - available) < 0.5
            && Math.Abs(_decidedForGap - Gap) < 0.5)
        {
            // Nothing this decision depends on has changed. The line keeps whatever it is already doing.
            return;
        }

        _decidedForText = Text;
        _decidedForWidth = available;
        _decidedForGap = Gap;

        ViewportClip.Rect = new Rect(0, 0, available, height);

        var natural = MeasureNaturalWidth();

        // One pass carries the track a whole copy of the value plus the gap that separates it from the copy behind
        // it. That distance is what makes the repeat seamless: the copy that follows lands exactly where the one
        // being watched began, so the pass that ends is indistinguishable from the pass that starts.
        var cycle = natural + Math.Max(0, Gap);

        _shouldScroll = natural - available > 1 && AnimationsEnabled();
        _cycle = _shouldScroll ? cycle : 0;

        // A line that cannot scroll is truncated instead: the readable answer for a reader who asked for less
        // motion, and for a value that simply cannot be shown in one line. It is given the field's own width, so
        // the alignment the caller asked for and the ellipsis are both measured against the field.
        //
        // A line that does scroll is given the width of the value it holds, and that width is load-bearing: the
        // track places the second copy after the first one's arranged width, so a first copy arranged narrower than
        // its text would put the second copy inside the first one's tail instead of a gap behind it.
        Line.TextTrimming = _shouldScroll ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        Line.Width = _shouldScroll ? natural : available;
        Echo.Width = natural;

        // The second copy is drawn only while the track moves. Standing still it would be a duplicate the reader
        // could see beyond the end of the value.
        Echo.Visibility = _shouldScroll ? Visibility.Visible : Visibility.Collapsed;

        ApplyScroll();
    }

    /// <summary>Starts the scroll if this line is a scroller and is on screen, and stops it otherwise.</summary>
    private void ApplyScroll()
    {
        if (_cycle > 1 && IsLoaded && !_isScrolling)
        {
            StartScrolling(_cycle);
        }
    }

    /// <summary>
    /// How wide the value wants to be. The trim is taken off for the moment: a line that is being cut down to the
    /// viewport reports the viewport's width, not the width of the value it is holding.
    /// </summary>
    private double MeasureNaturalWidth()
    {
        Line.Width = double.NaN;
        Line.TextTrimming = TextTrimming.None;
        Line.Measure(new Size(double.PositiveInfinity, Viewport.ActualHeight));

        return Line.DesiredSize.Width;
    }

    private void StartScrolling(double cycle)
    {
        // The compositor drives the track's Translation channel, which a XAML element only offers once it has been
        // switched on for that element. Doing it per pass is idempotent and keeps the switch next to its use.
        ElementCompositionPreview.SetIsTranslationEnabled(Track, true);

        var track = ElementCompositionPreview.GetElementVisual(Track);

        track.StopAnimation("Translation");

        var pass = track.Compositor.CreateVector3KeyFrameAnimation();
        pass.Duration = TimeSpan.FromSeconds(Math.Clamp(
            cycle / ScrollPixelsPerSecond,
            s_shortestPass.TotalSeconds,
            s_longestPass.TotalSeconds));

        // One steady slide, with no beat at either end: the track is carrying a second copy of the value a gap
        // behind the first, so the frame that ends a pass is the frame that began it, and a pause would only show
        // as the line stalling at a point in the text that is no more interesting than any other.
        pass.InsertKeyFrame(0f, Vector3.Zero);
        pass.InsertKeyFrame(1f, new Vector3((float)-cycle, 0f, 0f));
        pass.IterationBehavior = AnimationIterationBehavior.Forever;

        track.StartAnimation("Translation", pass);

        _isScrolling = true;
    }

    private void StopScrolling()
    {
        if (Track is null)
        {
            return;
        }

        try
        {
            // Stopping the animation returns the channel to its base value, which is the element's own Translation
            // of zero: no separate reset is needed, and none is writable from here. This also runs when the control
            // is leaving the tree, where the element visual may already be gone.
            ElementCompositionPreview.GetElementVisual(Track).StopAnimation("Translation");
        }
        catch (Exception)
        {
            // Nothing is running that could be stopped; there is no state left to correct either way.
        }

        _isScrolling = false;
    }

    /// <summary>
    /// The system's own answer to "animate things?". A failure to read it is not a request for less motion, so the
    /// documented default — the line moves — is used instead.
    /// </summary>
    private static bool AnimationsEnabled()
    {
        try
        {
            return s_uiSettings.Value.AnimationsEnabled;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
