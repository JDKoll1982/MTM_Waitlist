using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace MTM_Waitlist.Module_Shared.Controls;

/// <summary>
/// A line of status that rotates: the line on screen slips away and the line replacing it rises into its place.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a control rather than a <c>TextBlock</c>.</b> A value that is replaced between frames reads as a flicker,
/// and on a launch that is still working a flicker is indistinguishable from a fault. The transition is what makes
/// the surface look like it is still working, which is the whole job of the line it draws.
/// </para>
/// <para>
/// <b>The system's animation setting is obeyed.</b> With animations switched off the value is replaced outright.
/// Movement somebody has asked not to see is not a decoration to keep, and it is also what a person with a
/// vestibular sensitivity needs. The check is the same one the scrolling text control beside this one makes.
/// </para>
/// <para>
/// <b>A rapid succession of values settles on the last one.</b> Work can report faster than the transition runs,
/// so a change arriving mid-transition stops the one in flight and starts again from what is on screen. Without
/// that, a value arriving during the fade-out would be overwritten by the fade-out's own ending.
/// </para>
/// <para>
/// <b>It is announced rather than only drawn.</b> The value is a live region, so a screen reader reads the status
/// as it changes instead of leaving a blind reader with no account of a launch that is taking a while.
/// </para>
/// </remarks>
public sealed partial class RotatingTextView : UserControl
{
    /// <summary>The value shown on the line. Setting it rotates the line to it.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(RotatingTextView),
        new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// How the line is aligned within the control. It is declared here because a <see cref="UserControl"/> carries
    /// the font properties and not this one, so a caller sizing a status line for a centre-aligned surface would
    /// otherwise have nothing to set.
    /// </summary>
    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
        nameof(TextAlignment),
        typeof(TextAlignment),
        typeof(RotatingTextView),
        new PropertyMetadata(TextAlignment.Left));

    /// <summary>How far, in pixels, a line travels as it comes in and as it goes out.</summary>
    private const double TravelDistance = 10;

    /// <summary>How long one half of the rotation takes. The pair is short enough to read as one movement.</summary>
    private static readonly TimeSpan HalfDuration = TimeSpan.FromMilliseconds(140);

    /// <summary>Reused for the setting, which is a per-user value that costs a call to read.</summary>
    private static readonly Lazy<UISettings> s_uiSettings = new(() => new UISettings());

    /// <summary>
    /// The storyboard in flight, or <c>null</c> when the line is still. A change arriving mid-transition stops it
    /// and starts again from where the line actually is, so a new value is never overwritten by the ending of the
    /// transition it interrupted.
    /// </summary>
    private Storyboard? _running;

    /// <summary>Creates the control.</summary>
    public RotatingTextView()
    {
        InitializeComponent();

        // A status line is a live region: it changes without anybody asking, so a screen reader is told rather
        // than left to discover it.
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);

        // A UserControl is not itself an automation element - the line inside it is - so the caller's automation
        // id is carried down to the element that can actually be found. Without this a driven run has to locate
        // the status by its own text, which changes under it.
        RegisterPropertyChangedCallback(AutomationProperties.AutomationIdProperty, OnAutomationIdChanged);
        AutomationProperties.SetAutomationId(Line, AutomationProperties.GetAutomationId(this));
    }

    /// <summary>Carries the caller's automation id down to the line when it is set after construction.</summary>
    private void OnAutomationIdChanged(DependencyObject sender, DependencyProperty property)
        => AutomationProperties.SetAutomationId(Line, AutomationProperties.GetAutomationId(this));

    /// <summary>The value shown on the line.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>How the line is aligned within the control.</summary>
    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// Starts the rotation when the value changes. The first value is drawn without a transition, because there is
    /// nothing to rotate away from and a first line that faded in would look like a fault on an otherwise still
    /// screen.
    /// </summary>
    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is RotatingTextView view)
        {
            var hadPrevious = !string.IsNullOrEmpty((string?)args.OldValue);

            view.RotateTo((string?)args.NewValue ?? string.Empty, hadPrevious);
        }
    }

    /// <summary>Replaces the line, rotating to it when that is wanted and allowed.</summary>
    /// <param name="value">The line to show.</param>
    /// <param name="animate">Whether to rotate to it rather than replacing it outright.</param>
    private void RotateTo(string value, bool animate)
    {
        _running?.Stop();
        _running = null;

        if (!animate || !AnimationsEnabled())
        {
            Line.Text = value;
            Line.Opacity = 1;
            Slide.Y = 0;
            return;
        }

        // What is on screen slips up and goes, starting from wherever the last transition left it.
        var leaving = new Storyboard();
        Add(leaving, Line, "Opacity", Line.Opacity, 0);
        Add(leaving, Slide, "Y", Slide.Y, -TravelDistance);

        leaving.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_running, leaving))
            {
                // A newer value has taken over, so this ending belongs to a line nobody is waiting for.
                return;
            }

            Line.Text = value;
            Slide.Y = TravelDistance;

            var arriving = new Storyboard();
            Add(arriving, Line, "Opacity", 0, 1);
            Add(arriving, Slide, "Y", TravelDistance, 0);

            _running = arriving;
            arriving.Begin();
        };

        _running = leaving;
        leaving.Begin();
    }

    /// <summary>Adds one half of the rotation: a property, from one value to another, over the shared duration.</summary>
    /// <param name="storyboard">The storyboard being built.</param>
    /// <param name="target">
    /// The object that owns the property. It is passed rather than reached through a property path because the
    /// line's slide lives on the transform the line carries, not on a property of the line, and a path that named
    /// a named element could not resolve and threw while the surface was being built.
    /// </param>
    /// <param name="path">The property to animate, relative to <paramref name="target"/>.</param>
    /// <param name="from">The value it starts at.</param>
    /// <param name="to">The value it ends at.</param>
    private static void Add(Storyboard storyboard, DependencyObject target, string path, double from, double to)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(HalfDuration),

            // Eased so the movement settles rather than stopping dead, which is what makes it read as one
            // movement rather than as two.
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, path);

        storyboard.Children.Add(animation);
    }

    /// <summary>Whether the system is currently animating transitions, honouring the reader's own setting.</summary>
    private static bool AnimationsEnabled()
    {
        try
        {
            return s_uiSettings.Value.AnimationsEnabled;
        }
        catch (Exception)
        {
            // A setting that cannot be read is not a reason to fail a launch: the transition is dropped and the
            // value is still shown.
            return false;
        }
    }

    /// <summary>
    /// Keeps the clip the size of the control, so the line travelling in and out is cut off at the field rather
    /// than drawn over whatever sits beside it. Without this the clip stays empty and nothing is drawn at all.
    /// </summary>
    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs args)
        => ViewportClip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
}
