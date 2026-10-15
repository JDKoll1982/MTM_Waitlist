using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;

using Windows.System;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// The body of the status word's flyout: the four conditions a material handler can report, drawn from the one
/// vocabulary and operable without a pointer (FR-071, FR-075).
/// </summary>
/// <remarks>
/// The control decides nothing about who may open it and writes nothing of its own. Its host is the gated status
/// word, so the picker is only ever reached by the row's own material handler, and the choice it reports is
/// handed to the row, which invokes the single command that stores a condition (FR-070, FR-074, FR-076).
/// </remarks>
public sealed partial class WaitlistStatusFlyout : UserControl
{
    public static readonly DependencyProperty ConditionsProperty = DependencyProperty.Register(
        nameof(Conditions),
        typeof(IReadOnlyList<string>),
        typeof(WaitlistStatusFlyout),
        new PropertyMetadata(HandlerStatusCatalog.Conditions));

    public WaitlistStatusFlyout() => InitializeComponent();

    /// <summary>
    /// The conditions the flyout offers, in their settled order (FR-071).
    /// </summary>
    /// <remarks>
    /// Defaulted to the shared vocabulary and handed in by the card from the same value, so the picker and the
    /// card that reads a condition back cannot disagree about the four (FR-102).
    /// </remarks>
    public IReadOnlyList<string> Conditions
    {
        get => (IReadOnlyList<string>?)GetValue(ConditionsProperty) ?? HandlerStatusCatalog.Conditions;
        set => SetValue(ConditionsProperty, value);
    }

    /// <summary>The flyout's own heading and accessible name, from a resource key rather than a literal (FR-075, FR-104).</summary>
    public string FlyoutAccessibleName => "Waitlist_Card_StatusFlyout.Heading".GetLocalized();

    /// <summary>
    /// The one of the four conditions the handler chose, reported rather than written: whether the choice may be
    /// made at all is the host's gate, and the write is the row's own command (FR-070, FR-072, FR-076).
    /// </summary>
    public event EventHandler<string>? ConditionChosen;

    /// <summary>
    /// Places focus on the first choice, so a handler who opened the flyout with the keyboard is already inside
    /// it and can reach the other three without a pointer (FR-075).
    /// </summary>
    public void FocusFirstChoice() => FirstChoiceButton(ConditionChoices)?.Focus(FocusState.Programmatic);

    private void OnConditionChoiceClick(object sender, RoutedEventArgs e)
    {
        // Only the four conditions are ever reported: the vocabulary is closed at both ends, so a host cannot be
        // handed a value the store would refuse (FR-071, FR-102).
        if (sender is Button { Content: string condition } && HandlerStatusCatalog.IsCondition(condition))
        {
            ConditionChosen?.Invoke(this, condition);
        }
    }

    /// <summary>
    /// Moves between the choices with the arrow keys, so the four can be read through without a pointer. The
    /// activation itself is the button's own Enter or Space, which raises the same Click the pointer raises
    /// (FR-075).
    /// </summary>
    private void OnConditionChoiceKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var direction = e.Key switch
        {
            VirtualKey.Down or VirtualKey.Right => FocusNavigationDirection.Down,
            VirtualKey.Up or VirtualKey.Left => FocusNavigationDirection.Up,
            _ => (FocusNavigationDirection?)null,
        };

        if (direction is { } move && FocusManager.TryMoveFocus(move))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// The first realized choice, found through the visual tree because a templated item carries no name the
    /// code-behind can read.
    /// </summary>
    private static Button? FirstChoiceButton(DependencyObject? root)
    {
        if (root is null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            if (child is Button button)
            {
                return button;
            }

            if (FirstChoiceButton(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
