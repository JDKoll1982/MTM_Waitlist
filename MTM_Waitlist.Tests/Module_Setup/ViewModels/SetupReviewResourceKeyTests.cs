using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.ViewModels;

/// <summary>
/// The two setup-review labels that used to fall back to their own resource key name (UX-001; FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this asserts against the resource file rather than through the lookup.</b> The test host carries no PRI
/// index, so every key resolves to itself here (see <c>ResourceExtensionsTests</c>). A case written through
/// <c>GetLocalized</c> would therefore pass for a missing key as readily as for a present one, which is the defect
/// it is meant to catch. The statement is made where the value actually lives, and the call sites are read to
/// confirm they ask for those exact names.
/// </para>
/// <para>
/// <b>Why the names carry the <c>.Text</c> suffix.</b> These two sentences are declared once, under the name the
/// markup uses. A second dot-less name beside them is not something a resw file can hold: MakePri reads a name
/// containing a dot as a subtree plus a resource, so declaring both makes the same entity a resource and a scope
/// (PRI175). The code-side lookup asks for the declared name, which the repository already does everywhere a key
/// carries a segment (for example <c>"Mock_Indicator.AgeMinutes"</c>).
/// </para>
/// </remarks>
[TestClass]
public sealed class SetupReviewResourceKeyTests
{
    private const string ScrapNoneKey = "Setup_Review_Scrap_None.Text";
    private const string SubordinatePartsCountKey = "Setup_Review_Section_SubordinateParts_Count.Text";

    [TestMethod]
    public void ViewModel_SetupReviewResourceKeyTests_BothKeysResolveToASentenceRatherThanTheirOwnName()
    {
        var resources = ReadResources();

        foreach (var key in new[] { ScrapNoneKey, SubordinatePartsCountKey })
        {
            Assert.IsTrue(resources.ContainsKey(key), $"'{key}' is declared in the resource file, so the review can show words");
            Assert.IsFalse(string.IsNullOrWhiteSpace(resources[key]), $"'{key}' carries a sentence rather than a blank");
            Assert.AreNotEqual(key, resources[key], $"'{key}' resolves to its own name, which is what the review used to display");
        }
    }

    [TestMethod]
    public void ViewModel_SetupReviewResourceKeyTests_TheReviewAsksForThoseExactKeys()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "MTM_Waitlist.Setup",
            "ViewModels",
            "SetupReviewViewModel.cs"));

        foreach (var key in new[] { ScrapNoneKey, SubordinatePartsCountKey })
        {
            StringAssert.Contains(
                source,
                $"\"{key}\".GetLocalized()",
                $"the review looks up '{key}', the name the resource file actually declares");
        }
    }

    /// <summary>The application's resource file, keyed by resource name.</summary>
    private static Dictionary<string, string> ReadResources()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw");

        Assert.IsTrue(File.Exists(path), "the application's resource file is where the rest of the suite reads it");

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .Where(data => data.Attribute("name") is not null)
            .GroupBy(data => data.Attribute("name")!.Value, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Elements("value").FirstOrDefault()?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }
}
