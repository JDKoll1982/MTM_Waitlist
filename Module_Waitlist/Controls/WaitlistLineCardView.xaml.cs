using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Converters;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

using System.ComponentModel;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// One waitlist row: the four zones of the card, fed by the row it is handed and by nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The card draws what its <see cref="SampleOrder"/> carries and decides no business rule of its own. The one
/// thing it must work out for itself is the colour of the status column, because a brush cannot be bound from
/// an enum: the column's theme is read from the row and handed to the same palette every other status surface
/// uses, so the card and the rest of the application can never disagree about what a state looks like
/// (FR-019).
/// </para>
/// <para>
/// The card carries no print action (FR-067). Printing a label belongs to the request's own page, where the
/// detail a label needs is, so nothing here draws a control that cannot do anything and nothing here records a
/// TODO about one.
/// </para>
/// </remarks>
public partial class WaitlistLineCardView : UserControl
{
    /// <summary>
    /// The one status palette converter; it holds no state, so one instance serves every card.
    /// </summary>
    /// <remarks>
    /// The parameters that say which of the palette's readings is wanted — the status column's own brush rather
    /// than the badge's text brush, and which surface the card is drawn on — live on the converter, so the card
    /// and the converter cannot drift apart.
    /// </remarks>
    private static readonly RequestStatusToBrushConverter s_statusToBrushConverter = new();

    public static readonly DependencyProperty OrderProperty = DependencyProperty.Register(
        nameof(Order),
        typeof(SampleOrder),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null, OnOrderChanged));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty AccentSurfaceBrushProperty = DependencyProperty.Register(
        nameof(AccentSurfaceBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    /// <summary>The status column's brush: the one colour the status word and the time ring are drawn in.</summary>
    public static readonly DependencyProperty StatusColumnBrushProperty = DependencyProperty.Register(
        nameof(StatusColumnBrush),
        typeof(Brush),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(null));

    /// <summary>Whether the row carries a status word at all; a row with none draws no word (FR-018).</summary>
    public static readonly DependencyProperty HasStatusWordProperty = DependencyProperty.Register(
        nameof(HasStatusWord),
        typeof(bool),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(true));

    /// <summary>Whether the row names a requesting work centre; a row that names none draws no gap (FR-006).</summary>
    public static readonly DependencyProperty HasRequestingWorkCenterProperty = DependencyProperty.Register(
        nameof(HasRequestingWorkCenter),
        typeof(bool),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(true));

    /// <summary>Whether the row's identifier resolved; an unresolvable value is hidden (FR-101).</summary>
    public static readonly DependencyProperty HasIdentifierProperty = DependencyProperty.Register(
        nameof(HasIdentifier),
        typeof(bool),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(true));

    /// <summary>The ring's accessible name, which states which of the two things it measures (FR-016).</summary>
    public static readonly DependencyProperty RingAccessibleNameProperty = DependencyProperty.Register(
        nameof(RingAccessibleName),
        typeof(string),
        typeof(WaitlistLineCardView),
        new PropertyMetadata(string.Empty));

    private SampleOrder? _subscribedOrder;

    public WaitlistLineCardView()
    {
        InitializeComponent();

        // The time-derived texts are recomputed once a minute while the row is on screen, so the ring and the
        // text inside it must follow the same tick. Re-subscribing on Loaded keeps that working when the list
        // recycles this container.
        Loaded += OnCardLoaded;
        Unloaded += OnCardUnloaded;

        // The status column's colours have one value per surface, so a card already on screen has to re-resolve
        // them when the background theme changes under it rather than keeping the pair it was built with.
        ActualThemeChanged += OnActualThemeChanged;

        ApplyOrderState();
    }

    public SampleOrder? Order
    {
        get => (SampleOrder?)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush? AccentSurfaceBrush
    {
        get => (Brush?)GetValue(AccentSurfaceBrushProperty);
        set => SetValue(AccentSurfaceBrushProperty, value);
    }

    /// <summary>The colour the whole status column is drawn in: the word and the ring (FR-019).</summary>
    public Brush? StatusColumnBrush
    {
        get => (Brush?)GetValue(StatusColumnBrushProperty);
        set => SetValue(StatusColumnBrushProperty, value);
    }

    public bool HasStatusWord
    {
        get => (bool)GetValue(HasStatusWordProperty);
        set => SetValue(HasStatusWordProperty, value);
    }

    public bool HasRequestingWorkCenter
    {
        get => (bool)GetValue(HasRequestingWorkCenterProperty);
        set => SetValue(HasRequestingWorkCenterProperty, value);
    }

    public bool HasIdentifier
    {
        get => (bool)GetValue(HasIdentifierProperty);
        set => SetValue(HasIdentifierProperty, value);
    }

    public string RingAccessibleName
    {
        get => (string)GetValue(RingAccessibleNameProperty);
        set => SetValue(RingAccessibleNameProperty, value);
    }

    /// <summary>The `Open details` tooltip and accessible name, from the resource file rather than a literal (FR-104).</summary>
    public string OpenDetailsText => "Waitlist_Card_OpenDetails.Text".GetLocalized();

    /// <summary>
    /// The announcement that the status word is operable for this row's handler, from the resource file rather
    /// than a literal, so whether the word can be used is announced rather than left to be discovered (FR-075, FR-104).
    /// </summary>
    public string StatusWordHelpText => "Waitlist_Card_StatusWord.SetCondition".GetLocalized();

    /// <summary>
    /// The four conditions the status word's flyout offers, in their settled order. The flyout host carries
    /// them; the flyout's own body and behaviour belong to the status flyout control (FR-071).
    /// </summary>
    public IReadOnlyList<string> ConditionOptions => HandlerStatusCatalog.Conditions;

    private void OnCardLoaded(object sender, RoutedEventArgs e) => AttachOrder(Order);

    private void OnCardUnloaded(object sender, RoutedEventArgs e) => DetachOrder(_subscribedOrder);

    /// <summary>
    /// The condition the row's own material handler chose from the status word's flyout, handed on with the row
    /// it belongs to. The card writes nothing itself: the choice is the row's command to run (FR-070, FR-072).
    /// </summary>
    public event EventHandler<(SampleOrder Row, string Condition)>? StatusConditionChosen;

    /// <summary>
    /// Places focus on the flyout's first choice as it opens, so the picker is usable without a pointer (FR-075).
    /// </summary>
    private void OnStatusConditionFlyoutOpened(object? sender, object e) => StatusConditionPicker.FocusFirstChoice();

    /// <summary>
    /// Closes the flyout on the choice (FR-074) and hands the row with the chosen condition to the list, which
    /// invokes the one command that stores it (FR-070, FR-076).
    /// </summary>
    private void OnStatusConditionChosen(object? sender, string condition)
    {
        StatusConditionFlyout.Hide();

        if (Order is { } order && HandlerStatusCatalog.IsCondition(condition))
        {
            StatusConditionChosen?.Invoke(this, (order, condition));
        }
    }

    private void OnOpenDetailsClick(object sender, RoutedEventArgs e)
    {
        // The one path that opens the request from the card (FR-068). It goes through the application's own
        // navigation service, and it hands the row over first so the list's connected animation still runs.
        if (Order is not { } order)
        {
            return;
        }

        var navigationService = App.GetService<INavigationService>();
        navigationService.SetListDataItemForNextConnectedAnimation(order);
        navigationService.NavigateTo(typeof(WaitlistViewDetailViewModel).FullName!, order.Id);
    }

    private static void OnOrderChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WaitlistLineCardView control)
        {
            // Drop the previous row's subscription before taking the new one, or a recycled container would
            // keep repainting a row it no longer shows.
            control.DetachOrder(args.OldValue as SampleOrder);
            control.AttachOrder(args.NewValue as SampleOrder);
            control.ApplyOrderState();
        }
    }

    private void AttachOrder(SampleOrder? order)
    {
        if (order is null || ReferenceEquals(_subscribedOrder, order))
        {
            return;
        }

        order.PropertyChanged += OnOrderPropertyChanged;
        _subscribedOrder = order;
    }

    private void DetachOrder(SampleOrder? order)
    {
        if (order is null || !ReferenceEquals(_subscribedOrder, order))
        {
            return;
        }

        order.PropertyChanged -= OnOrderPropertyChanged;
        _subscribedOrder = null;
    }

    private void OnOrderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName;

        if (string.Equals(propertyName, nameof(SampleOrder.StatusWord), StringComparison.Ordinal))
        {
            HasStatusWord = !string.IsNullOrWhiteSpace(Order?.StatusWord);
        }

        if (string.Equals(propertyName, nameof(SampleOrder.StatusTheme), StringComparison.Ordinal))
        {
            UpdateStatusColumnBrush();
        }

        if (string.Equals(propertyName, nameof(SampleOrder.RingMeasure), StringComparison.Ordinal))
        {
            UpdateRingAccessibleName();
        }
    }

    /// <summary>
    /// Brings everything the card works out for itself in step with the row it now holds.
    /// </summary>
    private void ApplyOrderState()
    {
        var order = Order;

        HasStatusWord = !string.IsNullOrWhiteSpace(order?.StatusWord);
        HasRequestingWorkCenter = !string.IsNullOrWhiteSpace(order?.RequestedPressName);
        HasIdentifier = !string.IsNullOrWhiteSpace(order?.Subtitle);

        UpdateRingAccessibleName();
        UpdateStatusColumnBrush();
    }

    /// <summary>
    /// States which of the two things the ring measures, so a screen reader reads the same thing the ring is
    /// drawing: the time the request has left while it is live, the time it took once it has closed (FR-015,
    /// FR-016).
    /// </summary>
    private void UpdateRingAccessibleName() =>
        RingAccessibleName = (Order?.RingMeasure ?? TimeRingMeasure.TimeLeft) == TimeRingMeasure.TimeTaken
            ? "Waitlist_Card_Ring.TimeTaken".GetLocalized()
            : "Waitlist_Card_Ring.TimeLeft".GetLocalized();

    /// <summary>
    /// Repaints the status column when the surface under the card changes.
    /// </summary>
    private void OnActualThemeChanged(FrameworkElement sender, object args) => UpdateStatusColumnBrush();

    /// <summary>
    /// Paints the status column from the row's own theme and the surface the card is actually on, through the one
    /// status palette (FR-019).
    /// </summary>
    /// <remarks>
    /// The surface comes from the card's own <see cref="FrameworkElement.ActualTheme"/> rather than from the
    /// application, because the background theme is chosen per person and applied to the shell's element, so the
    /// two can differ. The palette answers a pair of colours for each of the three themes — one for a light card
    /// and one for a dark one — so the word and the ring stay legible on either.
    /// </remarks>
    private void UpdateStatusColumnBrush() =>
        StatusColumnBrush = s_statusToBrushConverter.Convert(
            Order?.StatusTheme ?? StatusColumnTheme.Normal,
            typeof(Brush),
            ActualTheme == ElementTheme.Dark
                ? RequestStatusToBrushConverter.StatusColumnThemeOnDarkParameter
                : RequestStatusToBrushConverter.StatusColumnThemeParameter,
            string.Empty) as Brush
            ?? RequestStatusPalette.BackgroundBrushFor(Order?.Status);
}
