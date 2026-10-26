using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// Picks the cell template for one detail cell of a card: the scrolling line for a cell whose value is a
/// description, the plain line for every other value (FR-028, FR-053).
/// </summary>
/// <remarks>
/// <para>
/// The choice belongs to the <b>cell</b>, not to the slot that holds it or to the Item that declares it. A
/// description is not always in the same slot — the third cell on a coil card, the first on a component card, and
/// whichever slot it lands in once a value ahead of it is missing — so a template pinned per slot would put the
/// scrolling line on whatever happened to sit there and clip the description anyway. Every slot therefore asks
/// this selector, and the cell's own label decides.
/// </para>
/// <para>
/// Both <see cref="SelectTemplateCore(object)"/> overloads are overridden on purpose. A <see cref="ContentControl"/>
/// asks the two-argument question, and the base implementation answers it with null — which makes the presenter
/// fall back to drawing its content, printing the cell's own type name where its label and value belong. That
/// exact fault shipped once in this file's sibling selector; see
/// <see cref="WaitlistLineCardDetailTemplateSelector"/>.
/// </para>
/// </remarks>
public sealed class WaitlistCardCellTemplateSelector : DataTemplateSelector
{
    /// <summary>The key of the plain cell template, as the dictionary declares it.</summary>
    public const string PlainTemplateKey = "WaitlistLineCardDetailCell";

    /// <summary>The key of the scrolling cell template, as the dictionary declares it.</summary>
    public const string ScrollingTemplateKey = "WaitlistLineCardDetailScrollingCell";

    /// <summary>The key of the pictured cell template, as the dictionary declares it.</summary>
    public const string PictureTemplateKey = "WaitlistLineCardDetailPictureCell";

    /// <summary>The cell drawn for a value that fits on one line, with its label beside it.</summary>
    public DataTemplate? PlainTemplate { get; set; }

    /// <summary>The cell drawn for a description, which scrolls its value past rather than clipping it.</summary>
    public DataTemplate? ScrollingTemplate { get; set; }

    /// <summary>
    /// The cell drawn for a value that carries a picture — the hopper's size and the scrap card's lugger — so the
    /// picture sits beside the word rather than in place of it (FR-077, FR-078).
    /// </summary>
    public DataTemplate? PictureTemplate { get; set; }

    /// <summary>
    /// Whether the given item is drawn by the pictured cell: true for a cell that pictures its value, false for
    /// every other cell and for anything that is not a cell at all.
    /// </summary>
    /// <param name="item">The item the presenter is drawing.</param>
    /// <returns><see langword="true"/> when the pictured cell is the right template for it.</returns>
    public static bool WantsPictureCell(object? item) => item is WaitlistLineCardCell { DrawsPicture: true };

    /// <summary>
    /// Whether the given item is drawn by the scrolling cell: true for a cell whose value is a description, false
    /// for every other cell and for anything that is not a cell at all.
    /// </summary>
    /// <param name="item">The item the presenter is drawing.</param>
    /// <returns><see langword="true"/> when the scrolling cell is the right template for it.</returns>
    public static bool WantsScrollingCell(object? item) => item is WaitlistLineCardCell { UsesScrollingText: true };

    /// <summary>The template for one cell.</summary>
    /// <param name="item">The item the presenter is drawing, expected to be a cell.</param>
    /// <returns>The pictured cell for a value with a picture, the scrolling one for a description, the plain one otherwise.</returns>
    protected override DataTemplate SelectTemplateCore(object item) => Resolve(item);

    /// <summary>
    /// The same resolution for the presenter that asks the two-argument question, which is every
    /// <see cref="ContentControl"/>.
    /// </summary>
    /// <param name="item">The item the presenter is drawing, expected to be a cell.</param>
    /// <param name="container">The element the presenter is drawing it into, which this selector does not use.</param>
    /// <returns>The pictured cell for a value with a picture, the scrolling one for a description, the plain one otherwise.</returns>
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Resolve(item);

    private DataTemplate Resolve(object item)
    {
        // A cell that pictures its value is drawn by the pictured cell and nothing else: a picture is what makes
        // that cell worth drawing, so falling back to a line without it would lose the thing being shown.
        if (WantsPictureCell(item) && PictureTemplate is { } picture)
        {
            return picture;
        }

        return (WantsScrollingCell(item) ? ScrollingTemplate : null)
            ?? PlainTemplate
            ?? ScrollingTemplate
            ?? PictureTemplate
            ?? EmptyTemplate;
    }

    /// <summary>
    /// The template drawn when neither of the selector's own templates can be resolved.
    /// </summary>
    /// <remarks>
    /// It draws nothing, which is the only correct answer: a cell that cannot find its template must lose its
    /// label and value silently rather than print a data type's name in their place. It is parsed from markup
    /// rather than built with <c>new DataTemplate()</c>, because a template built in code carries no content and a
    /// contentless template is exactly what lets the presenter fall back to rendering its content.
    /// </remarks>
    private static readonly DataTemplate EmptyTemplate =
        (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Grid /></DataTemplate>");
}
