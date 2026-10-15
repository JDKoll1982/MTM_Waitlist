using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The remembered answer to "is the picture share answering?", which is what keeps a card list off the network
/// while the share is silent.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect these pin.</b> A card's picture is looked for on the share whenever this computer holds no copy,
/// and that look is a file probe against a network path. On a workstation off the plant network the probe does not
/// fail quickly — it costs a failed name resolution, measured at hundreds of milliseconds to seconds — and a list
/// asks about every part it draws, once per accepted extension, on every rebuild. A single silent share therefore
/// stalled the list by seconds on every status change (2026-10-11, feature <c>012-waitlist-card-layout</c>).
/// </para>
/// <para>
/// <b>Why the rule is checked here rather than through a share.</b> Every case below is decided from a verdict and
/// a clock that the test supplies, so nothing here touches a network, and the window's two boundaries are stated
/// rather than waited for.
/// </para>
/// </remarks>
[TestClass]
public sealed class PictureShareReachabilityTests
{
    private static readonly DateTimeOffset s_start = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [TestInitialize]
    public void TestInitialize() => PictureShareReachability.Forget();

    [TestCleanup]
    public void TestCleanup() => PictureShareReachability.Forget();

    [TestMethod]
    public void Service_PictureShareReachabilityTests_TheShareIsTheServerAndShareNameRatherThanTheFile()
    {
        Assert.AreEqual(
            @"\\mtmanu-fs01\Expo Drive",
            PictureShareReachability.ShareRootOf(@"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Images\Material\coil\P-1.png"),
            "A file on a share is not judged by the share it is read from (FR-030).");

        // Forward slashes reach here from a stored path, and a share spelled that way still names one share.
        Assert.AreEqual(
            @"\\mtmanu-fs01\Expo Drive",
            PictureShareReachability.ShareRootOf(@"\\mtmanu-fs01\Expo Drive/Images/P-1.png"),
            "A share written with forward slashes is not recognised as the same share.");

        // A bare server names no share, so there is nothing a file probe could have been aimed at.
        Assert.IsNull(PictureShareReachability.ShareRootOf(@"\\mtmanu-fs01"), "A bare server name is treated as a share.");

        // A local path answers by its drive, and a drive is always there: nothing local is ever skipped.
        Assert.AreEqual(@"C:\", PictureShareReachability.ShareRootOf(@"C:\Images\P-1.png"), "A local file is not judged by its drive.");

        // A relative path has no root to read, so no verdict is invented for it.
        Assert.IsNull(PictureShareReachability.ShareRootOf(@"Images\P-1.png"), "A relative path is treated as a share.");
        Assert.IsNull(PictureShareReachability.ShareRootOf(null), "A path that is not there at all is treated as a share.");
        Assert.IsNull(PictureShareReachability.ShareRootOf("   "), "A blank path is treated as a share.");
    }

    [TestMethod]
    public void Service_PictureShareReachabilityTests_AShareThatDoesNotAnswerIsProbedOnceForTheWholeWindow()
    {
        var probes = 0;
        var now = s_start;

        bool Probe(string share)
        {
            probes++;
            return false;
        }

        for (var ask = 0; ask < 25; ask++)
        {
            // Twenty-five questions in a row is what one rebuild asks: every part, every extension.
            Assert.IsFalse(
                PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, now),
                "A share that did not answer was reported as answering.");
        }

        Assert.AreEqual(
            1,
            probes,
            "A silent share was probed once per question rather than once per window, which is the whole cost this class exists to remove.");
    }

    [TestMethod]
    public void Service_PictureShareReachabilityTests_AShareThatAnswersIsRememberedTheSameWay()
    {
        var probes = 0;
        var now = s_start;

        bool Probe(string share)
        {
            probes++;
            return true;
        }

        Assert.IsTrue(PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, now), "A share that answered was reported as silent.");
        Assert.IsTrue(PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, now), "A share that answered was reported as silent on the second question.");
        Assert.AreEqual(1, probes, "A share that answered was probed again inside the window, so the healthy case pays more than it has to.");

        // A share that answers is never skipped: the verdict only ever avoids a probe that would have failed.
        Assert.IsFalse(
            PictureShareReachability.ShouldProbe(now, now),
            "A verdict inside its window is probed again anyway, which would make the memo pointless.");
    }

    [TestMethod]
    public void Service_PictureShareReachabilityTests_TheShareIsProbedAgainOnceTheWindowLapses()
    {
        var probes = 0;
        var now = s_start;

        bool Probe(string share)
        {
            probes++;
            return probes > 1;
        }

        Assert.IsFalse(PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, now), "The first probe is not the one being tested.");

        // Still inside the window: the verdict stands and the share is not asked again.
        var insideTheWindow = now + PictureShareReachability.VerdictLifetime - TimeSpan.FromSeconds(1);
        Assert.IsFalse(
            PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, insideTheWindow),
            "The verdict was dropped before its window had lapsed.");
        Assert.AreEqual(1, probes, "The share was probed again inside the window.");

        // At the boundary and beyond: the share is asked again, so a workstation that rejoins the plant network
        // picks its pictures up by itself rather than needing a restart.
        var atTheBoundary = now + PictureShareReachability.VerdictLifetime;
        Assert.IsTrue(
            PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Expo Drive", Probe, atTheBoundary),
            "A share that came back was still reported silent once the window had lapsed.");
        Assert.AreEqual(2, probes, "The share was not probed again once the window had lapsed.");
    }

    [TestMethod]
    public void Service_PictureShareReachabilityTests_EachShareIsJudgedOnItsOwn()
    {
        var probed = new List<string>();

        bool Probe(string share)
        {
            probed.Add(share);
            return share.EndsWith(@"\Good", StringComparison.Ordinal);
        }

        Assert.IsFalse(PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Bad", Probe, s_start), "The silent share was reported as answering.");
        Assert.IsTrue(PictureShareReachability.IsReachable(@"\\mtmanu-fs01\Good", Probe, s_start), "The answering share was reported as silent.");

        CollectionAssert.AreEquivalent(
            new[] { @"\\mtmanu-fs01\Bad", @"\\mtmanu-fs01\Good" },
            probed,
            "One share's verdict was applied to another share.");
    }

    [TestMethod]
    public void Service_PictureShareReachabilityTests_APathWithNoShareToJudgeIsNeverSkipped()
    {
        var probes = 0;

        bool Probe(string share)
        {
            probes++;
            return false;
        }

        // Nothing to judge: no share root is found, so the gate is stepped over and the caller probes the file
        // itself exactly as it did before this class existed. A relative path is the case that matters — it is
        // read from this machine, and refusing it would lose a local picture.
        foreach (var pathWithNoShare in new string?[] { null, "  ", @"Images\P-1.png", @"\\mtmanu-fs01" })
        {
            Assert.IsNull(
                PictureShareReachability.ShareRootOf(pathWithNoShare),
                $"'{pathWithNoShare}' was read as a share, so a path with nothing to ask about would be gated on a verdict it has no share for.");
        }

        // And the verdict itself answers yes when it is handed nothing to judge, so the gate is a no-op rather
        // than a refusal.
        foreach (var nothing in new string?[] { null, "  ", string.Empty })
        {
            Assert.IsTrue(
                PictureShareReachability.IsReachable(nothing, Probe, s_start),
                "A share root that is not there was refused, which would hide a picture that is on this machine.");
        }

        Assert.AreEqual(0, probes, "A path with no share was probed as though it had one.");
    }
}
