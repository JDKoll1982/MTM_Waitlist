using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// Both work-center selection screens draw at most three cards across.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a ceiling at all.</b> The pages fill the window now, so the number of cards in a row would otherwise grow with
/// the monitor. Measuring against the width floor of 300 px and the default gap, a 3456-px window solved to nine
/// columns on each grid; at three the row still reads as a row of choices rather than as a wall of thumbnails.
/// </para>
/// <para>
/// The ceiling itself is the solver's (<c>CardGridLayout.Solve</c> takes a maximum column count and already has a test
/// for it). What this pins is that both screens ask for it, on both of their grids — the hot list and the rest — because
/// the two grids are declared separately and one of them being left uncapped is invisible until the window is wide.
/// </para>
/// </remarks>
[TestClass]
public sealed class WorkCenterGridCeilingMarkupTests
{
    /// <summary>The column count both screens cap at.</summary>
    private const int Ceiling = 3;

    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace s_shared = "using:MTM_Waitlist.Module_Shared.Services";

    private static readonly XName s_name = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml").GetName("Name");

    /// <summary>The two screens that pick a work centre, addressed from the repository root.</summary>
    private static readonly string[][] s_pages =
    [
        ["Module_Setup", "Views", "SetupWorkCenterPage.xaml"],
        ["Module_Waitlist", "Views", "NewRequestWorkCenterPage.xaml"],
    ];

    [TestMethod]
    public void Audit_WorkCenterGridCeilingMarkupTests_BothScreensCapEveryCardGridAtThree()
    {
        foreach (var page in s_pages)
        {
            var grids = Load(page)
                .Descendants(s_presentation + "GridView")
                .Where(grid => grid.Attribute(s_shared + "CardGridSizing.MinimumCardWidth") is not null)
                .ToList();

            Assert.AreEqual(
                2,
                grids.Count,
                $"{string.Join("/", page)} no longer declares the two card grids this audit reads, so the ceiling below "
                    + "cannot be checked");

            foreach (var grid in grids)
            {
                Assert.AreEqual(
                    Ceiling.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    (string?)grid.Attribute(s_shared + "CardGridSizing.MaximumColumns"),
                    $"{string.Join("/", page)}: '{(string?)grid.Attribute(s_name) ?? "a card grid"}' has no column "
                        + $"ceiling, so a wide window draws more than {Ceiling} cards across");
            }
        }
    }

    private static XDocument Load(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the markup this audit reads is not where it was: {path}");

        return XDocument.Load(path);
    }
}
