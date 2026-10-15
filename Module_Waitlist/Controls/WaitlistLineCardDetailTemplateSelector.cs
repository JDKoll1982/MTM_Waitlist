using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// Picks the detail template for the row the card is drawing: one template per Item, keyed on the row's own
/// Item code, with the card's own fallback when the Item has no template (FR-025, FR-103).
/// </summary>
/// <remarks>
/// <para>
/// The twenty-four templates live in one resource dictionary (<c>WaitlistLineCardDetailTemplates.xaml</c>), not
/// in one control per Item: a <see cref="UserControl"/> needs a file of its own, and template selection by an
/// item's own properties is the documented WinUI mechanism for choosing between them (research R3, D31).
/// </para>
/// <para>
/// The template is looked up by key at run time rather than referenced from the card's markup, so an Item's
/// template landing in the dictionary is all it takes for that Item to draw its own cells; the card does not
/// have to be reopened. Both the application's own resources and the dictionaries it merges are searched, so
/// the per-Item templates can be either.
/// </para>
/// <para>
/// The fallback is the one case the card must still draw something for: an Item the dictionary carries no
/// template for draws no cells at all - no substituted value and no explanation (FR-060). The card declares
/// that fallback under <see cref="FallbackTemplateKey"/>.
/// </para>
/// </remarks>
public sealed class WaitlistLineCardDetailTemplateSelector : DataTemplateSelector
{
    /// <summary>The prefix every per-Item detail template's key starts with.</summary>
    public const string TemplateKeyPrefix = "WaitlistLineCardDetail_";

    /// <summary>The key of the template drawn when the row's Item has none of its own.</summary>
    public const string FallbackTemplateKey = "WaitlistLineCardDetailFallback";

    /// <summary>
    /// The template drawn for a row whose Item has no template of its own. The card sets it from its own
    /// resources, so the fallback is a real template rather than nothing.
    /// </summary>
    public DataTemplate? FallbackTemplate { get; set; }

    /// <summary>
    /// The resource key a row's Item is looked up by, or empty when the row names no Item.
    /// </summary>
    /// <param name="itemCode">The row's Item code, for example <c>pickup-coil</c>.</param>
    /// <returns>The Item's own template key, or empty when there is no Item to key on.</returns>
    public static string TemplateKeyFor(string? itemCode) =>
        string.IsNullOrWhiteSpace(itemCode) ? string.Empty : TemplateKeyPrefix + itemCode.Trim();

    /// <summary>
    /// The template to draw the given item with: the row's own Item's template when the host can see one,
    /// otherwise the fallback.
    /// </summary>
    /// <param name="item">The item the presenter is drawing, expected to be a row.</param>
    /// <returns>The template to draw with.</returns>
    protected override DataTemplate SelectTemplateCore(object item) => Resolve(item);

    /// <summary>
    /// The same resolution, for the presenter that asks the two-argument question.
    /// </summary>
    /// <param name="item">The item the presenter is drawing, expected to be a row.</param>
    /// <param name="container">The element the presenter is drawing it into, which this selector does not use.</param>
    /// <returns>The template to draw with.</returns>
    /// <remarks>
    /// <b>Both overloads have to be overridden, and overriding only the one-argument one is what broke the cells.</b>
    /// A <see cref="ContentControl"/> asks its selector the <i>two</i>-argument question. Leaving that overload
    /// alone means the base implementation answers it, and the base answers null - so the presenter is handed no
    /// template at all and falls back to drawing its content, which for a row is the row's own type name. That is
    /// why every card read `MTM_Waitlist.Module_Waitlist.Models.SampleOrder` where its cells belong, and why no
    /// cell label was ever drawn: the per-Item templates were being resolved correctly and then thrown away.
    /// A unit test cannot see this, because the framework is what asks the question - it took reading the running
    /// card's automation tree to find it.
    /// </remarks>
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Resolve(item);

    /// <summary>
    /// The one resolution both overloads answer with.
    /// </summary>
    /// <param name="item">The item the presenter is drawing, expected to be a row.</param>
    /// <returns>The row's Item's template, or the fallback.</returns>
    private DataTemplate Resolve(object item)
    {
        var key = TemplateKeyFor((item as SampleOrder)?.ItemCode);

        var template = key.Length == 0 ? null : Lookup(Application.Current?.Resources, key);

        // The fallback is a template with an empty root, never `new DataTemplate()`: a contentless template lets
        // the presenter fall back to rendering its content, which prints the row's type name where the cells go.
        return template ?? FallbackTemplate ?? EmptyTemplate;
    }

    /// <summary>
    /// The template drawn when neither the Item's own template nor the card's fallback can be resolved.
    /// </summary>
    /// <remarks>
    /// It draws nothing, which is the only correct answer here: a row whose template cannot be found must lose its
    /// cells silently rather than show a data type's name in their place. It is parsed from markup rather than
    /// built with <c>new DataTemplate()</c>, because a template built in code carries no content and a contentless
    /// template is exactly what lets the presenter fall back to rendering its content.
    /// </remarks>
    private static readonly DataTemplate EmptyTemplate =
        (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Grid /></DataTemplate>");

    /// <summary>
    /// Looks a template up by key in a dictionary and in every dictionary it merges.
    /// </summary>
    /// <param name="dictionary">The dictionary to search, or null when there is no application to ask.</param>
    /// <param name="key">The template's key.</param>
    /// <returns>The template, or null when neither the dictionary nor a merged one holds it.</returns>
    /// <remarks>
    /// The key is matched exactly first and then <b>case-insensitively</b>. Resource keys are case-sensitive, so
    /// an Item code whose stored spelling differs in case from the template's key would otherwise resolve to no
    /// template at all - and a row with no template loses its cells silently, which is the one failure this
    /// lookup must not have. Every other comparison of an Item code in this module is case-insensitive, so
    /// matching this one the same way keeps the two from disagreeing about which Item a row is.
    /// </remarks>
    private static DataTemplate? Lookup(ResourceDictionary? dictionary, string key)
    {
        if (dictionary is null)
        {
            return null;
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (Lookup(merged, key) is DataTemplate fromMerged)
            {
                return fromMerged;
            }
        }

        if (dictionary.TryGetValue(key, out var found) && found is DataTemplate exact)
        {
            return exact;
        }

        foreach (var candidate in dictionary.Keys)
        {
            if (candidate is string candidateKey
                && string.Equals(candidateKey, key, StringComparison.OrdinalIgnoreCase)
                && dictionary.TryGetValue(candidateKey, out var insensitive)
                && insensitive is DataTemplate matched)
            {
                return matched;
            }
        }

        return null;
    }
}
