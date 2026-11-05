using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Controls;

/// <summary>
/// Draws one step: its heading, exactly one of the three presentations, and the two ways of leaving it.
/// </summary>
/// <remarks>
/// The three presentations are drawn in one place so a step cannot show two of them at once, and so the retry
/// control exists only alongside the unavailable state. Every string is resolved from a resource key, which is why
/// the frame is handed keys rather than text.
/// </remarks>
public sealed partial class GenericRequestStepFrame : UserControl
{
    public static readonly DependencyProperty TitleKeyProperty = DependencyProperty.Register(
        nameof(TitleKey), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty IntroKeyProperty = DependencyProperty.Register(
        nameof(IntroKey), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty EmptyKeyProperty = DependencyProperty.Register(
        nameof(EmptyKey), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(object), typeof(GenericRequestStepFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty PresentationProperty = DependencyProperty.Register(
        nameof(Presentation),
        typeof(GenericRequestPresentation),
        typeof(GenericRequestStepFrame),
        new PropertyMetadata(GenericRequestPresentation.Empty, OnTextChanged));

    public static readonly DependencyProperty AttemptedAtDisplayProperty = DependencyProperty.Register(
        nameof(AttemptedAtDisplay), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(
        nameof(RetryCommand), typeof(ICommand), typeof(GenericRequestStepFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty ContinueCommandProperty = DependencyProperty.Register(
        nameof(ContinueCommand), typeof(ICommand), typeof(GenericRequestStepFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty BackCommandProperty = DependencyProperty.Register(
        nameof(BackCommand), typeof(ICommand), typeof(GenericRequestStepFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty ContinueKeyProperty = DependencyProperty.Register(
        nameof(ContinueKey), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata("GenericRequest_StepFrame_Continue.Content", OnTextChanged));

    public static readonly DependencyProperty BackVisibleProperty = DependencyProperty.Register(
        nameof(BackVisible), typeof(bool), typeof(GenericRequestStepFrame), new PropertyMetadata(true, OnTextChanged));

    public static readonly DependencyProperty ContinueVisibleProperty = DependencyProperty.Register(
        nameof(ContinueVisible), typeof(bool), typeof(GenericRequestStepFrame), new PropertyMetadata(true, OnTextChanged));

    public static readonly DependencyProperty LoadingTextProperty = DependencyProperty.Register(
        nameof(LoadingText), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty UnavailableTextProperty = DependencyProperty.Register(
        nameof(UnavailableText), typeof(string), typeof(GenericRequestStepFrame), new PropertyMetadata(string.Empty, OnTextChanged));

    public GenericRequestStepFrame()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The resource key of the forward button's label. A step whose forward action is something else says so here.
    /// </summary>
    public string ContinueKey
    {
        get => (string)GetValue(ContinueKeyProperty);
        set => SetValue(ContinueKeyProperty, value);
    }

    /// <summary>Whether the step can be left backwards. The first step cannot.</summary>
    public bool BackVisible
    {
        get => (bool)GetValue(BackVisibleProperty);
        set => SetValue(BackVisibleProperty, value);
    }

    /// <summary>Whether the step has a forward action at all.</summary>
    public bool ContinueVisible
    {
        get => (bool)GetValue(ContinueVisibleProperty);
        set => SetValue(ContinueVisibleProperty, value);
    }

    /// <summary>
    /// What the frame says while the read is in flight, when a step has its own words for it. Empty keeps the ring
    /// on its own, which is what the steps that have no such line show.
    /// </summary>
    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    /// <summary>
    /// The step's own sentence for a failed read, already carrying its attempt time. When it is set the frame draws
    /// it in place of its generic heading and its separate attempt line, so the same moment is not named twice;
    /// when it is empty every other step keeps the heading and attempt line it has always had.
    /// </summary>
    public string UnavailableText
    {
        get => (string)GetValue(UnavailableTextProperty);
        set => SetValue(UnavailableTextProperty, value);
    }

    /// <summary>The resource key of this step's heading.</summary>
    public string TitleKey
    {
        get => (string)GetValue(TitleKeyProperty);
        set => SetValue(TitleKeyProperty, value);
    }

    /// <summary>The resource key of this step's introduction.</summary>
    public string IntroKey
    {
        get => (string)GetValue(IntroKeyProperty);
        set => SetValue(IntroKeyProperty, value);
    }

    /// <summary>The resource key of what this step says when the read found nothing.</summary>
    public string EmptyKey
    {
        get => (string)GetValue(EmptyKeyProperty);
        set => SetValue(EmptyKeyProperty, value);
    }

    /// <summary>The step's own content, shown only while the presentation is populated.</summary>
    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public GenericRequestPresentation Presentation
    {
        get => (GenericRequestPresentation)GetValue(PresentationProperty);
        set => SetValue(PresentationProperty, value);
    }

    public string AttemptedAtDisplay
    {
        get => (string)GetValue(AttemptedAtDisplayProperty);
        set => SetValue(AttemptedAtDisplayProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public ICommand? ContinueCommand
    {
        get => (ICommand?)GetValue(ContinueCommandProperty);
        set => SetValue(ContinueCommandProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public string TitleText => Resolve(TitleKey);

    public string IntroText => Resolve(IntroKey);

    public string EmptyText => Resolve(EmptyKey);

    /// <summary>Whether the read is in flight, which is what keeps the progress ring turning.</summary>
    public bool IsLoading => Presentation == GenericRequestPresentation.Loading;

    public Visibility LoadingVisibility => Presentation == GenericRequestPresentation.Loading
        ? Visibility.Visible
        : Visibility.Collapsed;

    /// <summary>The loading line is drawn only when a step has one of its own.</summary>
    public Visibility LoadingTextVisibility => IsLoading && string.IsNullOrWhiteSpace(LoadingText) is false
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility BodyVisibility => Presentation is GenericRequestPresentation.Loading
        ? Visibility.Collapsed
        : Presentation == GenericRequestPresentation.Populated
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility EmptyVisibility => Presentation is GenericRequestPresentation.Loading
        ? Visibility.Collapsed
        : Presentation == GenericRequestPresentation.Empty
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility UnavailableVisibility => Presentation is GenericRequestPresentation.Loading
        ? Visibility.Collapsed
        : Presentation == GenericRequestPresentation.Unavailable
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>The frame's own heading, drawn when the step supplied no sentence of its own.</summary>
    public Visibility UnavailableHeadingVisibility => Presentation == GenericRequestPresentation.Unavailable
        && string.IsNullOrWhiteSpace(UnavailableText)
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>The step's own sentence for a failed read, when it has one.</summary>
    public Visibility UnavailableMessageVisibility => Presentation == GenericRequestPresentation.Unavailable
        && string.IsNullOrWhiteSpace(UnavailableText) is false
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>The attempt time, drawn beneath the frame's own heading and folded into a step's own sentence.</summary>
    public Visibility UnavailableAttemptedVisibility => Presentation == GenericRequestPresentation.Unavailable
        && string.IsNullOrWhiteSpace(UnavailableText)
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>The button labels, read for the automation name as well as drawn as the content.</summary>
    public string BackName => "GenericRequest_StepFrame_Back.Content".GetLocalized();

    public string ContinueName => (string.IsNullOrWhiteSpace(ContinueKey)
        ? "GenericRequest_StepFrame_Continue.Content"
        : ContinueKey).GetLocalized();

    public Visibility BackVisibility => BackVisible ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ContinueVisibility => ContinueVisible ? Visibility.Visible : Visibility.Collapsed;

    public string RetryName => "GenericRequest_StepFrame_Retry.Content".GetLocalized();

    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((GenericRequestStepFrame)sender).Bindings.Update();

    private static string Resolve(string key) => string.IsNullOrWhiteSpace(key) ? string.Empty : key.GetLocalized();
}
