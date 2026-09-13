using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

/// <summary>
/// The seed round-trip proof for <c>seed_setup_active_jobs_eight_configurations</c> (§D18).
/// <para>
/// It reads the seeded <c>subordinate_parts_json</c> back through <see cref="ActiveJobItemResolverService"/>'s
/// <b>own</b> deserializer, so the eight-configuration matrix is a proof about the real read path rather than
/// about a hand-written expectation of a seed shape.
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
    public async Task Seeded900_1_CoilOnly_NormalisesToCoil()
    {
        var snapshot = await ResolveSeededAsync("900-1");

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual("MMC0001000", snapshot.Coils[0].PartNumber);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
        Assert.AreEqual(0, snapshot.Dies.Count);
    }

    [TestMethod]
    public async Task Seeded900_2_FlatstockTaggedComponent_IsNormalisedByThePartNumberPrefix()
    {
        // The row is deliberately stored with Category 'Component'; the MMF prefix is authoritative, so the
        // real read path must land it in Flatstock. This is the job that proves a flatstock-only job is
        // offered pickup-coil (D21).
        var snapshot = await ResolveSeededAsync("900-2");

        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual("MMF0001154", snapshot.Flatstock[0].PartNumber);
        Assert.AreEqual(0, snapshot.Components.Count, "The MMF prefix must win over the stored Component tag.");
    }

    [TestMethod]
    public async Task Seeded900_3_DieOnly_NormalisesToDie()
    {
        var snapshot = await ResolveSeededAsync("900-3");

        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task Seeded900_4_ComponentOnly_KeepsTheStoredCategory()
    {
        var snapshot = await ResolveSeededAsync("900-4");

        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.AreEqual(0, snapshot.Coils.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task Seeded900_5_DunnageOnly_HasNoSubordinatePart()
    {
        var snapshot = await ResolveSeededAsync("900-5");

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0, "900-5 assigns a dunnage part.");
    }

    [TestMethod]
    public async Task Seeded900_6_EverythingAtOnce_NormalisesEveryCategory()
    {
        var snapshot = await ResolveSeededAsync("900-6");

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0);
    }

    [TestMethod]
    public async Task Seeded900_7_EmptySubordinateArray_ResolvesWithNothingPresent()
    {
        var snapshot = await ResolveSeededAsync("900-7");

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.AreEqual(0, snapshot.DunnageParts.Count);
    }

    [TestMethod]
    public async Task Seeded900_8_WorkCentreWithNoRow_ReadsAsNoActiveJob()
    {
        // The eighth configuration is realised by the ABSENCE of a row, and "no active job" is what the
        // resolver must answer for it.
        var snapshot = await ResolveAsync("900-8");

        Assert.IsNull(snapshot);
    }

    [TestMethod]
    public async Task SeededScrapValues_CoverTheWholeThreeWayGate()
    {
        // A real type (900-1), the placeholder (900-3) and 'No Scrap' (900-4) — the input FR-031 is written
        // against — must survive the round trip verbatim, because the picker's scrap gate reads them.
        Assert.IsTrue(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync("900-1")).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync("900-3")).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveSeededAsync("900-4")).SubordinateParts[0].SelectedScrapType));
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
