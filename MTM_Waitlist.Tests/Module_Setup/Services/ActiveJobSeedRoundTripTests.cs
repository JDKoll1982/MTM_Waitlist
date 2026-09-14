using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

/// <summary>
/// The seed round-trip proof for <c>seed_setup_active_jobs_eight_configurations</c> (§D18, FR-039).
/// <para>
/// It reads the seeded <c>subordinate_parts_json</c> back through <see cref="ActiveJobItemResolverService"/>'s
/// <b>own</b> deserializer, so the eight-configuration matrix is a proof about the real read path rather than
/// about a hand-written expectation of a seed shape.
/// </para>
/// <para>
/// The matrix now lives on <b>real</b> work centres — five Expo Drive and two Vits Drive — rather than on the
/// seven <c>900-*</c> fixture stations, which have retired. The job shapes did not change, so the expectations
/// below are the same claims about the same situations, asserted against the work centres that carry them now.
/// </para>
/// <para>
/// Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>: without a reachable
/// <c>mtm_waitlist</c> every test reports inconclusive rather than failing, so the suite stays green offline.
/// Weakened never — an inconclusive test is recorded as skipped, not as passing.
/// </para>
/// </summary>
[TestClass]
public sealed class ActiveJobSeedRoundTripTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    // The seven situations, on the work centres the seed's own header records (FR-039).
    private const string CoilOnly = "100-3";
    private const string FlatstockOnly = "100-6";
    private const string DieOnly = "100-7";
    private const string ComponentOnly = "100-18";
    private const string DunnageOnly = "100-1806";
    private const string EverythingAtOnce = "V100-33";
    private const string NoSubordinatePart = "V100-34";

    /// <summary>The eighth case: a work centre with no active job, realised by the absence of a row.</summary>
    private const string WorkCentreWithNoActiveJob = "100-8";

    /// <summary>The retired fixtures. No job may point at any of them any more (FR-039, SC-016).</summary>
    private static readonly string[] s_retiredFixtureWorkCentres =
        { "900-1", "900-2", "900-3", "900-4", "900-5", "900-6", "900-7" };

    private ActiveJobItemResolverService? _resolver;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        var helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
        _resolver = new ActiveJobItemResolverService(helper);
    }

    [TestMethod]
    public async Task Seeded100_3_CoilOnly_NormalisesToCoil()
    {
        var snapshot = await ResolveSeededAsync(CoilOnly);

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual("MMC0001000", snapshot.Coils[0].PartNumber);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
        Assert.AreEqual(0, snapshot.Dies.Count);
    }

    [TestMethod]
    public async Task Seeded100_6_FlatstockTaggedComponent_IsNormalisedByThePartNumberPrefix()
    {
        // The row is deliberately stored with Category 'Component'; the MMF prefix is authoritative, so the
        // real read path must land it in Flatstock. This is the job that proves a flatstock-only job is
        // offered pickup-coil (D21).
        var snapshot = await ResolveSeededAsync(FlatstockOnly);

        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual("MMF0001154", snapshot.Flatstock[0].PartNumber);
        Assert.AreEqual(0, snapshot.Components.Count, "The MMF prefix must win over the stored Component tag.");
    }

    [TestMethod]
    public async Task Seeded100_7_DieOnly_NormalisesToDie()
    {
        var snapshot = await ResolveSeededAsync(DieOnly);

        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task Seeded100_18_ComponentOnly_KeepsTheStoredCategory()
    {
        var snapshot = await ResolveSeededAsync(ComponentOnly);

        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.AreEqual(0, snapshot.Coils.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task Seeded100_1806_DunnageOnly_HasNoSubordinatePart()
    {
        var snapshot = await ResolveSeededAsync(DunnageOnly);

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0, $"{DunnageOnly} assigns a dunnage part.");
    }

    [TestMethod]
    public async Task SeededV100_33_EverythingAtOnce_NormalisesEveryCategory()
    {
        var snapshot = await ResolveSeededAsync(EverythingAtOnce);

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0);
    }

    [TestMethod]
    public async Task SeededV100_34_EmptySubordinateArray_ResolvesWithNothingPresent()
    {
        var snapshot = await ResolveSeededAsync(NoSubordinatePart);

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.AreEqual(0, snapshot.DunnageParts.Count);
    }

    [TestMethod]
    public async Task Seeded100_8_WorkCentreWithNoRow_ReadsAsNoActiveJob()
    {
        // The eighth configuration is realised by the ABSENCE of a row, and "no active job" is what the
        // resolver must answer for it.
        var snapshot = await ResolveAsync(WorkCentreWithNoActiveJob);

        Assert.IsNull(snapshot);
    }

    [TestMethod]
    public async Task Retired900Fixtures_ResolveToNoActiveJob()
    {
        // FR-039 / SC-016: the seven fixture stations have retired and their situations moved to real work
        // centres. This is the data-side half of that claim — a fixture name resolving to a job would mean a
        // stale row survived, whatever the seed file says.
        foreach (var retired in s_retiredFixtureWorkCentres)
        {
            Assert.IsNull(
                await ResolveAsync(retired),
                $"The retired fixture work centre '{retired}' must not carry an active job any more (FR-039, SC-016).");
        }
    }

    [TestMethod]
    public async Task SeededScrapValues_CoverTheWholeThreeWayGate()
    {
        // A real type (coil only), the placeholder (die only) and 'No Scrap' (component only) — the input
        // FR-031 is written against — must survive the round trip verbatim, because the picker's scrap gate
        // reads them.
        Assert.IsTrue(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync(CoilOnly)).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync(DieOnly)).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync(ComponentOnly)).SubordinateParts[0].SelectedScrapType));
    }

    private async Task<SetupActiveJobSnapshot> ResolveSeededAsync(string workCenter)
    {
        var snapshot = await ResolveAsync(workCenter);
        Assert.IsNotNull(snapshot, $"The seed must carry a row for work centre '{workCenter}'.");
        return snapshot!;
    }

    private Task<SetupActiveJobSnapshot?> ResolveAsync(string workCenter) =>
        _resolver!.ResolveAsync(workCenter);
}
