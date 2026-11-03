using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// Picks the card a board row is drawn with: the generic request's own card for a generic request, and the work-order
/// card for every other row (016-generic-wc-request-foundation, FR-022).
/// </summary>
/// <remarks>
/// <para>
/// Both cards sit in one list because a handler works one queue, and the row's own stored flag is what decides which
/// card states its facts. A row is never drawn with the other kind's card: the work-order card would draw empty
/// work-order, coil, flatstock and component cells where a generic request has nothing to say (FR-022, FR-025).
/// </para>
/// <para>
/// <b>Both overloads are overridden.</b> A presenter asks the two-argument question, and leaving that one to the base
/// class answers null, which makes the presenter draw its content - the row's type name - instead of the card. That is
/// the failure this repository already found and fixed once in
/// <see cref="WaitlistLineCardDetailTemplateSelector"/>, and it is repeated here deliberately.
/// </para>
/// </remarks>
public sealed class GenericRequestCardTemplateSelector : DataTemplateSelector
{
    /// <summary>The template drawn for a generic request.</summary>
    public DataTemplate? GenericTemplate { get; set; }

    /// <summary>The template drawn for a work-order request.</summary>
    public DataTemplate? WorkOrderTemplate { get; set; }

    /// <summary>The template to draw the given item with.</summary>
    /// <param name="item">The item the presenter is drawing, expected to be a board row.</param>
    /// <returns>The row's own kind's template.</returns>
    protected override DataTemplate? SelectTemplateCore(object item) => Resolve(item);

    /// <summary>The same resolution, for the presenter that asks the two-argument question.</summary>
    /// <param name="item">The item the presenter is drawing, expected to be a board row.</param>
    /// <param name="container">The element the presenter is drawing it into, which this selector does not use.</param>
    /// <returns>The row's own kind's template.</returns>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) => Resolve(item);

    private DataTemplate? Resolve(object item) =>
        item is SampleOrder { IsGeneric: true } ? GenericTemplate : WorkOrderTemplate;
}
