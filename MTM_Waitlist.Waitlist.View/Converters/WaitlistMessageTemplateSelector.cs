using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Draws a message on the side its sender sits on: the signed-in person's own messages right, everyone else's left.
/// </summary>
/// <remarks>
/// Two templates rather than one template with two bindings, because the two sides differ in more than alignment —
/// their bubble colours and their text colours are different theme brushes, and a theme brush can only be named
/// statically. Each template therefore carries its own <c>ThemeResource</c> values, which is what keeps both sides
/// readable in the light and the dark theme without a converter looking them up at run time.
/// </remarks>
public sealed class WaitlistMessageTemplateSelector : DataTemplateSelector
{
    /// <summary>The template for a message the signed-in person sent.</summary>
    public DataTemplate? MineTemplate { get; set; }

    /// <summary>The template for a message anyone else sent.</summary>
    public DataTemplate? TheirTemplate { get; set; }

    /// <inheritdoc />
    protected override DataTemplate? SelectTemplateCore(object item) =>
        SelectTemplateCore(item, null);

    /// <inheritdoc />
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        item is WaitlistMessageRow { IsMine: true } ? MineTemplate : TheirTemplate;
}
