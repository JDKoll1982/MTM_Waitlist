using Microsoft.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Converters;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Tests.Module_Mock;

using Windows.UI;

namespace MTM_Waitlist.Tests.Module_Waitlist.Converters;

/// <summary>
/// The status column's colour: the three column themes, and the lighter red a handler's condition is drawn in.
/// The column is painted from one brush, so the word and the ring cannot disagree, and a theme the converter
/// answers nothing for would leave both invisible — which is the defect this pins shut.
/// </summary>
/// <remarks>
/// The converter's decision is read as a colour rather than through <c>IValueConverter.Convert</c>, because that
/// method can only finish by building a <c>SolidColorBrush</c>, and a WinUI brush cannot be built outside a UI
/// thread: a probe in this very host fails with <c>COMException</c> ("For UWP projects, if you are using UI
/// objects in test consider using [UITestMethod]"). The one-line seam that turns the decision into the brush is
/// asserted against the artifact that ships, which is this repository's answer for app-project surfaces.
/// </remarks>
[TestClass]
public sealed class RequestStatusToBrushConverterTests
{
    /// <summary>The parameter the card passes when it wants the whole column's theme brush.</summary>
    private const string ThemeParameter = "StatusColumnTheme";

    private const string ConverterPath = "MTM_Waitlist.Waitlist.View/Converters/RequestStatusToBrushConverter.cs";

    /// <summary>The converter itself, for the one reading that does not build a brush.</summary>
    private static readonly RequestStatusToBrushConverter s_converter = new();

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_ResolvesARealColourForEveryOneOfTheEightRoleStates()
    {
        // FR-102: eight states, each with a colour of its own to be drawn in. A role the converter answers
        // nothing for is a status the column draws nothing for.
        foreach (var word in TheEightStatusWords())
        {
            var role = RequestStatusPalette.RoleFor(word);

            Assert.AreNotEqual(
                Colors.Transparent,
                RequestStatusToBrushConverter.ColourFor(role, parameter: null),
                $"'{word}' resolves to a role with no colour, so the column would draw nothing for it (FR-102).");
        }

        Assert.AreNotEqual(
            Colors.Transparent,
            RequestStatusToBrushConverter.ColourFor(RequestStatusBadgeRole.Overdue, parameter: null),
            "The overdue marking resolves to a role with no colour, so an overdue column would draw nothing (FR-014).");
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_ResolvesARealColourForEachOfTheThreeColumnThemes()
    {
        // FR-019: three themes, and the card asks for them by theme. Answering nothing is the loose end US1 left:
        // the card fell back to the lifecycle colour because the theme came back transparent.
        var expected = new (StatusColumnTheme Theme, Color Colour)[]
        {
            (StatusColumnTheme.Normal, RequestStatusPalette.NormalColumnColor),
            (StatusColumnTheme.HandlerCondition, RequestStatusPalette.HandlerConditionColumnColor),
            (StatusColumnTheme.Overdue, RequestStatusPalette.OverdueColumnColor),
        };

        foreach (var (theme, colour) in expected)
        {
            var resolved = RequestStatusToBrushConverter.ColourFor(theme, ThemeParameter);

            Assert.AreNotEqual(
                Colors.Transparent,
                resolved,
                $"The {theme} column theme resolves to no colour, so the card's stand-in is taken instead (FR-019).");
            Assert.AreEqual(colour, resolved, $"The {theme} column theme is not the palette's colour for it (FR-019).");
        }
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_TheNormalThemeIsItsOwnColourAndNotTheOverdueOne()
    {
        // SC-007: overdue has to be unmistakable, so an ordinary column is never painted as an overdue one.
        var normal = RequestStatusToBrushConverter.ColourFor(StatusColumnTheme.Normal, ThemeParameter);
        var overdue = RequestStatusToBrushConverter.ColourFor(StatusColumnTheme.Overdue, ThemeParameter);

        Assert.AreEqual(RequestStatusPalette.NormalColumnColor, normal, "The ordinary column is not drawn in the normal theme (FR-019).");
        Assert.AreNotEqual(normal, overdue, "An ordinary column and an overdue one are painted the same colour (FR-020).");
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_PlacesAConditionInTheLighterRedAndOverdueInTheDeeperOne()
    {
        // FR-072: a condition is a lighter red than overdue. Lighter in fact, not only in the name, because the two
        // must never be confusable (SC-007).
        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            Assert.AreEqual(
                RequestStatusPalette.HandlerConditionColumnColor,
                RequestStatusToBrushConverter.ColourFor(RequestStatusPalette.RoleFor(condition), ThemeParameter),
                $"'{condition}' is not drawn in the handler-condition theme (FR-072).");
        }

        Assert.IsTrue(
            RelativeLuminance(RequestStatusPalette.HandlerConditionColumnColor) > RelativeLuminance(RequestStatusPalette.OverdueColumnColor),
            "The condition red is not lighter than the overdue red, so the two themes are not the pair FR-072 describes.");
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_AcceptsAStatusWordOrAStoredStatusOnTheSamePath()
    {
        // The default path is what a caller with only the row's status to hand uses. The stored status and the word
        // the card draws must land on the same colour, or copy and data would paint differently.
        Assert.AreEqual(
            RequestStatusPalette.ColorFor(RequestStatusBadgeRole.Waiting),
            RequestStatusToBrushConverter.ColourFor("Pending", parameter: null),
            "The stored Waiting status and the word the card draws are not the same colour (FR-012).");

        Assert.AreEqual(
            RequestStatusPalette.HandlerConditionColumnColor,
            RequestStatusToBrushConverter.ColourFor(HandlerStatusCatalog.Conditions[0], parameter: null),
            "A condition handed over as its stored value is not drawn in the handler-condition theme (FR-072).");
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_AnUnrecognisedStatusPaintsNothing()
    {
        // FR-018: a status row with no value is removed rather than blanked, so nothing must be painted for it.
        foreach (var missing in new object?[] { null, string.Empty, "Archived", RequestStatusBadgeRole.None })
        {
            Assert.AreEqual(
                Colors.Transparent,
                RequestStatusToBrushConverter.ColourFor(missing, parameter: null),
                $"'{missing?.ToString() ?? "null"}' is not a state the column knows and must paint nothing (FR-018).");
        }
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_TheForegroundParameterStillAnswersTheTextColour()
    {
        // The card's status word may be drawn in the palette's text colour; that reading must survive the theme
        // reading being added beside it.
        Assert.AreEqual(
            RequestStatusPalette.BadgeForeground,
            RequestStatusToBrushConverter.ColourFor("Pending", "Foreground"),
            "The Foreground reading no longer answers the palette's text colour.");
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_ConvertBackIsRefusedRatherThanGuessed()
    {
        // A colour cannot be turned back into a status, and a converter that invented one would be inventing data.
        Assert.ThrowsException<NotSupportedException>(
            () => s_converter.ConvertBack(RequestStatusPalette.NormalColumnColor, typeof(string), null!, string.Empty));
    }

    [TestMethod]
    public void Converter_RequestStatusToBrushConverterTests_TurnsTheColourItDecidedIntoTheBrushTheColumnBinds()
    {
        // The decision is only useful if the column is painted with it. Read as the artifact that ships, because
        // finishing the conversion means building a brush, and a brush cannot be built in this host.
        var source = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            ConverterPath.Replace('/', Path.DirectorySeparatorChar)));

        StringAssert.Contains(
            source,
            "new SolidColorBrush(ColourFor(value, parameter))",
            "Convert no longer paints with the colour it decided, so the column binds a brush nobody chose.");
    }

    /// <summary>
    /// The eight status words FR-102 settles, read from the one vocabulary rather than re-listed (FR-104).
    /// </summary>
    private static IReadOnlyList<string> TheEightStatusWords()
    {
        var words = new List<string> { "Waiting", "In Progress", "Done", "Cancelled" };
        words.AddRange(HandlerStatusCatalog.Conditions);
        return words;
    }

    private static double RelativeLuminance(Color colour) =>
        (0.2126 * Channel(colour.R)) + (0.7152 * Channel(colour.G)) + (0.0722 * Channel(colour.B));

    private static double Channel(byte value)
    {
        var scaled = value / 255.0;
        return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }
}
