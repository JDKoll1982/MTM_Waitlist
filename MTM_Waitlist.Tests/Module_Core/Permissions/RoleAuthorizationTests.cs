using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// The ladder readers as Phase 3 leaves them (FR-016, FR-017, FR-018).
/// </summary>
/// <remarks>
/// Every rank below is spelled out on purpose: a test that read the ladder from the catalogue could not catch a
/// rung the catalogue got wrong. The ranks are the ones the specification pins, so this file is the place a
/// changed rung shows up as a failure rather than as a quiet difference in behaviour.
/// </remarks>
[TestClass]
public sealed class RoleAuthorizationTests
{
    /// <summary>
    /// The pinned ladder: code and rung, exactly as the contract states them.
    /// </summary>
    private static readonly (string Code, int Rank)[] PinnedLadder =
    {
        ("developer", 100),
        ("it_department", 90),
        ("plant_manager", 80),
        ("production_lead", 70),
        ("setup_lead", 60),
        ("material_handler_lead", 50),
        ("material_handler", 10),
        ("production", 10),
        ("setup", 10),
    };

    /// <summary>The three roles that share one rung.</summary>
    private static readonly string[] WorkerCodes = { "material_handler", "production", "setup" };

    /// <summary>The eight codes the shipped seed holds while the retired role is still in the catalogue.</summary>
    private static readonly string[] LiveCodesWithoutTheRetiredRole =
    {
        "developer", "plant_manager", "production_lead", "setup_lead",
        "material_handler_lead", "material_handler", "production", "setup",
    };

    [TestMethod]
    public void IsAtLeast_AgreesAtAndAroundEveryBoundary()
    {
        var ladder = Ladder();

        foreach (var held in ladder)
        {
            Assert.IsTrue(
                RoleAuthorization.IsAtLeast(held, held),
                $"{held.RoleCode} must be at least itself.");

            foreach (var other in ladder)
            {
                var expected = held.RoleRank >= other.RoleRank;

                Assert.AreEqual(
                    expected,
                    RoleAuthorization.IsAtLeast(held, other),
                    $"{held.RoleCode} ({held.RoleRank}) at least {other.RoleCode} ({other.RoleRank}).");

                // Strictly-above is the same comparison with the boundary excluded, so the two can never
                // disagree about a peer.
                Assert.AreEqual(
                    held.RoleRank > other.RoleRank,
                    RoleAuthorization.IsAbove(held, other),
                    $"{held.RoleCode} above {other.RoleCode}.");
            }
        }
    }

    [TestMethod]
    public void IsAtLeast_PeersAtOneRungAreMutuallyAtOrAboveAndNobodyIsAboveTheirPeer()
    {
        var workers = Ladder().Where(entry => WorkerCodes.Contains(entry.RoleCode)).ToArray();

        Assert.AreEqual(3, workers.Length);
        Assert.AreEqual(1, workers.Select(entry => entry.RoleRank).Distinct().Count());

        foreach (var left in workers)
        {
            foreach (var right in workers)
            {
                Assert.IsTrue(RoleAuthorization.IsAtLeast(left, right));
                Assert.IsFalse(RoleAuthorization.IsAbove(left, right), $"{left.RoleCode} is not above {right.RoleCode}.");
            }
        }
    }

    [TestMethod]
    public void AtOrAbove_ReturnsEveryRoleAtThatRungOrHigherInCatalogueOrder()
    {
        var ladder = Ladder();

        var atOrAboveProductionLead = RoleAuthorization.AtOrAbove(ladder, Find(ladder, "production_lead"));

        CollectionAssert.AreEqual(
            new[] { "developer", "it_department", "plant_manager", "production_lead" },
            atOrAboveProductionLead.Select(entry => entry.RoleCode).ToArray());
    }

    [TestMethod]
    public void AtOrAbove_TheLowestRungReturnsTheWholeCatalogue()
    {
        var ladder = Ladder();

        var atOrAboveTheWorkerRung = RoleAuthorization.AtOrAbove(ladder, Find(ladder, "setup"));

        CollectionAssert.AreEqual(
            ladder.Select(entry => entry.RoleCode).ToArray(),
            atOrAboveTheWorkerRung.Select(entry => entry.RoleCode).ToArray());
    }

    [TestMethod]
    public void AtOrBelow_IsTheMirrorOfAtOrAbove()
    {
        var ladder = Ladder();

        var atOrBelowPlantManager = RoleAuthorization.AtOrBelow(ladder, Find(ladder, "plant_manager"));
        var atOrAboveProductionLead = RoleAuthorization.AtOrAbove(ladder, Find(ladder, "production_lead"));

        CollectionAssert.AreEqual(
            new[]
            {
                "plant_manager", "production_lead", "setup_lead", "material_handler_lead",
                "material_handler", "production", "setup",
            },
            atOrBelowPlantManager.Select(entry => entry.RoleCode).ToArray());

        // The two lists overlap on exactly the roles that sit between the two boundaries, so a role can be
        // inserted between two rungs without either list losing an entry.
        var below = atOrBelowPlantManager.Select(entry => entry.RoleCode).ToArray();
        var above = atOrAboveProductionLead.Select(entry => entry.RoleCode).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "plant_manager", "production_lead" },
            below.Intersect(above, StringComparer.Ordinal).ToArray());

        Assert.AreEqual(ladder.Count, below.Union(above, StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void Highest_TakesTheHighestRungWhenAPersonHoldsMoreThanOneLiveRole()
    {
        var ladder = Ladder();

        var highest = RoleAuthorization.Highest(
            new[] { Find(ladder, "setup"), Find(ladder, "plant_manager"), Find(ladder, "production") });

        Assert.IsNotNull(highest);
        Assert.AreEqual("plant_manager", highest!.RoleCode);
    }

    [TestMethod]
    public void Highest_WithOneRoleReturnsThatRole()
    {
        var ladder = Ladder();

        var highest = RoleAuthorization.Highest(new[] { Find(ladder, "material_handler_lead") });

        Assert.IsNotNull(highest);
        Assert.AreEqual("material_handler_lead", highest!.RoleCode);
    }

    [TestMethod]
    public void Highest_BreaksATieOnTheCodeSoTwoPeersAlwaysAnswerTheSameWay()
    {
        var ladder = Ladder();

        var highest = RoleAuthorization.Highest(
            new[] { Find(ladder, "setup"), Find(ladder, "material_handler"), Find(ladder, "production") });

        Assert.IsNotNull(highest);
        Assert.AreEqual("material_handler", highest!.RoleCode);
    }

    [TestMethod]
    public void Highest_WithNoRoleReturnsNothing()
    {
        Assert.IsNull(RoleAuthorization.Highest(Array.Empty<RoleCatalogEntry>()));
    }

    [TestMethod]
    public void TheLadder_IsTheEightLiveCodesWhileTheRetiredRoleIsStillInTheCatalogue()
    {
        // Phase 3's catalogue holds the eight codes the shipped seed ranks. The retired role is still in the
        // catalogue at this point and the catalogue read excludes it, which is what this asserts: the ladder
        // readers are handed what the read returned, and it holds no retired code.
        var live = Ladder().Where(entry => LiveCodesWithoutTheRetiredRole.Contains(entry.RoleCode)).ToArray();

        Assert.AreEqual(8, live.Length);
        CollectionAssert.DoesNotContain(live.Select(entry => entry.RoleCode).ToArray(), "admin");
        CollectionAssert.DoesNotContain(live.Select(entry => entry.RoleCode).ToArray(), "it_department");
    }

    private static IReadOnlyList<RoleCatalogEntry> Ladder() =>
        PinnedLadder
            .Select((entry, index) => new RoleCatalogEntry(index + 1, entry.Code, entry.Code, entry.Rank))
            .ToArray();

    private static RoleCatalogEntry Find(IEnumerable<RoleCatalogEntry> ladder, string roleCode) =>
        ladder.Single(entry => string.Equals(entry.RoleCode, roleCode, StringComparison.Ordinal));
}
