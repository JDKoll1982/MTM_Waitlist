using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The shared step frame can say that it is still reading, and says nothing else while it is.
/// </summary>
/// <remarks>
/// <para>
/// The frame is a WinUI control and this suite runs without a XAML runtime, so the check reads the frame's own
/// markup and code rather than instantiating it. That is the established way the surfaces in this repository are
/// audited, and it is enough to pin the rule the requirement states: the ring is drawn exactly while the
/// presentation is <c>Loading</c>, and the body, the empty text and the unavailable panel are all collapsed then
/// (FR-010).
/// </para>
/// <para>
/// A still-loading frame must never show the empty text, because that sentence tells the requester the plant holds
/// nothing — which is a finished read's answer, not a read in progress.
/// </para>
/// </remarks>
[TestClass]
public sealed class GenericRequestStepFrameLoadingTests
{
    private const string FrameXaml = "MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml";
    private const string FrameSource = "MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml.cs";

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Markup_GenericRequestStepFrameLoadingTests_TheFrameDrawsTheProgressRingFromTheLoadingState()
    {
        var markup = Read(FrameXaml);

        StringAssert.Contains(
            markup,
            "<ProgressRing",
            "The frame draws no progress ring, so a step cannot say that it is still reading.");

        StringAssert.Contains(
            markup,
            "Visibility=\"{x:Bind LoadingVisibility}\"",
            "The ring is not tied to the loading presentation, so it would be drawn in a settled state too.");

        StringAssert.Contains(
            markup,
            "IsActive=\"{x:Bind IsLoading}\"",
            "An inactive ring is invisible even when it is present, so the loading state would show nothing.");
    }

    [TestMethod]
    public void Code_GenericRequestStepFrameLoadingTests_TheLoadingStateHidesTheBodyAndBothSettledPanels()
    {
        var source = Read(FrameSource);

        foreach (var property in new[] { "BodyVisibility", "EmptyVisibility", "UnavailableVisibility" })
        {
            var body = PropertyBody(source, property);

            StringAssert.Contains(
                body,
                "GenericRequestPresentation.Loading",
                $"{property} does not consider the loading state, so it would be drawn while the read is in flight.");

            StringAssert.Contains(
                body,
                "Visibility.Collapsed",
                $"{property} does not collapse while the read is in flight, so a still-loading frame would claim "
                    + "a finished read.");
        }
    }

    [TestMethod]
    public void Code_GenericRequestStepFrameLoadingTests_TheRingIsVisibleOnlyWhileTheReadIsInFlight()
    {
        var source = Read(FrameSource);

        var body = PropertyBody(source, "LoadingVisibility");

        StringAssert.Contains(body, "GenericRequestPresentation.Loading");
        StringAssert.Contains(body, "Visibility.Visible");
        StringAssert.Contains(body, "Visibility.Collapsed");

        var isActive = PropertyBody(source, "IsLoading");
        StringAssert.Contains(
            isActive,
            "GenericRequestPresentation.Loading",
            "The ring's active state is not the loading presentation, so it would spin or stop at the wrong time.");
    }

    /// <summary>The declaration of one computed property, from its name to the semicolon that ends it.</summary>
    private static string PropertyBody(string source, string propertyName)
    {
        var match = Regex.Match(
            source,
            $@"\b{Regex.Escape(propertyName)}\s*=>[\s\S]*?;",
            RegexOptions.CultureInvariant);

        Assert.IsTrue(match.Success, $"the frame declares no '{propertyName}', so this guard is watching nothing.");

        return match.Value;
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
