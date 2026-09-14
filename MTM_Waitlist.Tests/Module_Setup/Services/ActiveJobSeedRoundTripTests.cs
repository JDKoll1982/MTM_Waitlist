using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

/// <summary>
/// The eight-configuration round-trip proof for the active-job read path (§D18, FR-039): the seven job shapes
/// that <c>seed_setup_active_jobs_eight_configurations</c> deploys, plus the no-row case, read back through
/// <see cref="ActiveJobItemResolverService"/>'s <b>own</b> deserializer.
/// <para>
/// <b>The fixtures are this test's own.</b> Every row is written here, with hard-coded values, under a work
/// centre named for this run, and deleted again in <see cref="TestCleanupAsync"/>. The test deliberately does
/// <b>not</b> read the deployed seed's rows: the live database changes underneath it. A live Setup save on any
/// of the work centres the seed names overwrites that centre's row — the seed's own header records that as an
/// accepted trade — and a test that turns red because the shop floor did its job is worse than no test at all.
/// The shapes below are the seed's shapes, copied verbatim, so the matrix still proves the real read path
/// rather than a hand-written expectation of it.
/// </para>
/// <para>
/// Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>: without a reachable
/// <c>mtm_waitlist</c> every test reports inconclusive rather than failing, so the suite stays green offline.
/// Weakened never — an inconclusive test is recorded as skipped, not as passing.
/// </para>
/// <para>
/// The one claim that still reads live rows is <see cref="Retired900Fixtures_ResolveToNoActiveJob"/>: its
/// subject is the <b>absence</b> of rows at the retired fixture names, so it writes nothing and needs no
/// fixture of its own.
/// </para>
/// </summary>
[TestClass]
public sealed class ActiveJobSeedRoundTripTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    /// <summary>
    /// Prefix for every work centre this test writes, so a run's rows are recognisable and removable. Each
    /// name also carries a per-run id, so two concurrent runs cannot delete each other's rows.
    /// </summary>
    private const string OwnedWorkCenterPrefix = "IT-ROUNDTRIP-";

    /// <summary>The retired fixtures. No job may point at any of them any more (FR-039, SC-016).</summary>
    private static readonly string[] s_retiredFixtureWorkCentres =
        { "900-1", "900-2", "900-3", "900-4", "900-5", "900-6", "900-7" };

    // The seven shapes, copied from seed_setup_active_jobs_eight_configurations. The keys are PascalCase
    // because SetupPersistenceService serializes the models directly; a hand-written shape that merely looked
    // plausible would make this a test of itself.

    /// <summary>Coil only: an <c>MMC</c> subordinate with a real scrap type.</summary>
    private const string CoilOnlySubordinateJson =
        """
        [{"Category":"Coil","PartNumber":"MMC0001000","Description":"Coil 0.062 x 48 wide","Location":"A-01-01","OnHandQuantity":1200.50,"User8":"","SelectedScrapType":"Steel Offal","IsLowStock":false}]
        """;

    /// <summary>Flatstock only: an <c>MMF</c> subordinate stored with the Category 'Component', deliberately.</summary>
    private const string FlatstockOnlySubordinateJson =
        """
        [{"Category":"Component","PartNumber":"MMF0001154","Description":"Flatstock 0.125 x 36","Location":"B-02-03","OnHandQuantity":88.00,"User8":"","SelectedScrapType":"No Scrap","IsLowStock":false}]
        """;

    /// <summary>Die only: an <c>FGT</c> subordinate carrying the 'Scrap Type Required' placeholder.</summary>
    private const string DieOnlySubordinateJson =
        """
        [{"Category":"Die","PartNumber":"FGT0002000","Description":"Die 9003-A","Location":"DIE SHOP","OnHandQuantity":0,"User8":"","SelectedScrapType":"Scrap Type Required","IsLowStock":false}]
        """;

    /// <summary>Component only: an unprefixed part stored as 'Component' with a real 'No Scrap' answer.</summary>
    private const string ComponentOnlySubordinateJson =
        """
        [{"Category":"Component","PartNumber":"CMP0004455","Description":"Bushing, bronze","Location":"C-04-12","OnHandQuantity":240.00,"User8":"","SelectedScrapType":"No Scrap","IsLowStock":false}]
        """;

    /// <summary>Dunnage only: no subordinate part at all, and one assigned dunnage part.</summary>
    private const string DunnageOnlySubordinateJson = "[]";

    private const string DunnageOnlyDunnageJson =
        """
        [{"Id":"DNG-PART-01","TypeId":"DNG-TYPE-01","PartNumber":"DNG0007788","DisplayName":"Rack, 24 x 36 wire","DunnageTypeName":"Rack","HomeLocation":"DUNNAGE-ROW-3","ImagePath":"","Metadata":"","IsSelectedForPair":true}]
        """;

    /// <summary>Everything at once: coil, flatstock, die and component, plus dunnage.</summary>
    private const string EverythingAtOnceSubordinateJson =
        """
        [{"Category":"Coil","PartNumber":"MMC0001001","Description":"Coil 0.048 x 36 wide","Location":"A-01-07","OnHandQuantity":640.00,"User8":"","SelectedScrapType":"Aluminum Offal","IsLowStock":false},{"Category":"Flatstock","PartNumber":"MMF0001155","Description":"Flatstock 0.090 x 24","Location":"B-02-08","OnHandQuantity":12.00,"User8":"","SelectedScrapType":"No Scrap","IsLowStock":true},{"Category":"Die","PartNumber":"FGT0002001","Description":"Die 9006-B","Location":"PRESS BAY","OnHandQuantity":0,"User8":"","SelectedScrapType":"Scrap Type Required","IsLowStock":false},{"Category":"Component","PartNumber":"CMP0004456","Description":"Pin, dowel 8mm","Location":"C-04-15","OnHandQuantity":0,"User8":"","SelectedScrapType":"","IsLowStock":false}]
        """;

    private const string EverythingAtOnceDunnageJson =
        """
        [{"Id":"DNG-PART-02","TypeId":"DNG-TYPE-02","PartNumber":"DNG0007789","DisplayName":"Tote, collapsible","DunnageTypeName":"Tote","HomeLocation":"DUNNAGE-ROW-4","ImagePath":"","Metadata":"","IsSelectedForPair":true}]
        """;

    private readonly List<string> _ownedWorkCenters = [];

    private MySqlHelperServer? _helper;
    private ActiveJobItemResolverService? _resolver;

    private string _coilOnly = string.Empty;
    private string _flatstockOnly = string.Empty;
    private string _dieOnly = string.Empty;
    private string _componentOnly = string.Empty;
    private string _dunnageOnly = string.Empty;
    private string _everythingAtOnce = string.Empty;
    private string _noSubordinatePart = string.Empty;

    /// <summary>The eighth case: a work centre with no active job, realised by the absence of a row.</summary>
    private string _workCentreWithNoActiveJob = string.Empty;

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
        _resolver = new ActiveJobItemResolverService(_helper);

        var runId = Guid.NewGuid().ToString("N")[..8];
        _coilOnly = $"{OwnedWorkCenterPrefix}{runId}-coil";
        _flatstockOnly = $"{OwnedWorkCenterPrefix}{runId}-flatstock";
        _dieOnly = $"{OwnedWorkCenterPrefix}{runId}-die";
        _componentOnly = $"{OwnedWorkCenterPrefix}{runId}-component";
        _dunnageOnly = $"{OwnedWorkCenterPrefix}{runId}-dunnage";
        _everythingAtOnce = $"{OwnedWorkCenterPrefix}{runId}-all";
        _noSubordinatePart = $"{OwnedWorkCenterPrefix}{runId}-empty";
        _workCentreWithNoActiveJob = $"{OwnedWorkCenterPrefix}{runId}-none";

        // The seven rows the matrix needs. Nothing is written for _workCentreWithNoActiveJob on purpose: the
        // eighth case is the absence of a row, and a name no run has ever used is the only honest way to
        // realise it on a database whose work centres carry real jobs.
        Assert.AreEqual(1, await InsertJobAsync(_coilOnly, "WO-RT-0001", "PART-RT-0001", "10", null, null, CoilOnlySubordinateJson, null), "The coil-only fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_flatstockOnly, "WO-RT-0002", "PART-RT-0002", "10", null, null, FlatstockOnlySubordinateJson, null), "The flatstock-only fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_dieOnly, "WO-RT-0003", "PART-RT-0003", "10", null, null, DieOnlySubordinateJson, null), "The die-only fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_componentOnly, "WO-RT-0004", "PART-RT-0004", "10", null, null, ComponentOnlySubordinateJson, null), "The component-only fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_dunnageOnly, "WO-RT-0005", "PART-RT-0005", "10", "DNG-TYPE-01", "DNG-PART-01", DunnageOnlySubordinateJson, DunnageOnlyDunnageJson), "The dunnage-only fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_everythingAtOnce, "WO-RT-0006", "PART-RT-0006", "20", "DNG-TYPE-02", "DNG-PART-02", EverythingAtOnceSubordinateJson, EverythingAtOnceDunnageJson), "The everything-at-once fixture row must insert.");
        Assert.AreEqual(1, await InsertJobAsync(_noSubordinatePart, "WO-RT-0007", "PART-RT-0007", "10", null, null, "[]", null), "The empty-subordinate fixture row must insert.");
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is null || _ownedWorkCenters.Count == 0)
        {
            // Setup reported inconclusive before writing anything, so there is nothing to remove.
            return;
        }

        // Delete the exact names this run created. A LIKE on the prefix would also reach into a concurrently
        // running instance's rows.
        var parameters = new Dictionary<string, object?>();
        var placeholders = new List<string>();
        for (var index = 0; index < _ownedWorkCenters.Count; index++)
        {
            var parameterName = $"p_work_center_{index}";
            placeholders.Add($"@{parameterName}");
            parameters[parameterName] = _ownedWorkCenters[index];
        }

        await _helper.ExecuteSqlNonQueryAsync(
            $"DELETE FROM setup_active_jobs WHERE work_center IN ({string.Join(", ", placeholders)});",
            parameters,
            MySqlDatabaseTarget.MtmWaitlist);
    }

    /// <summary>
    /// Writes one fixture row and records its work centre for teardown, using the same columns the deployed
    /// seed writes through <c>sp_setup_save_setup</c>.
    /// </summary>
    private async Task<int> InsertJobAsync(
        string workCenter,
        string workOrder,
        string partNumber,
        string sequenceNumber,
        string? selectedDunnageTypeId,
        string? selectedDunnagePartId,
        string subordinatePartsJson,
        string? dunnagePartsJson)
    {
        _ownedWorkCenters.Add(workCenter);

        return await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO setup_active_jobs (public_id, work_order, part_number, sequence_number, work_center, selected_dunnage_type_id, selected_dunnage_part_id, subordinate_parts_json, selected_dunnage_parts_json, is_active, created_utc, updated_utc) "
                + "VALUES (@p_public_id, @p_work_order, @p_part_number, @p_sequence, @p_work_center, @p_dunnage_type, @p_dunnage_part, @p_sub_parts, @p_dunnage_parts, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_public_id"] = Guid.NewGuid().ToString(),
                ["p_work_order"] = workOrder,
                ["p_part_number"] = partNumber,
                ["p_sequence"] = sequenceNumber,
                ["p_work_center"] = workCenter,
                ["p_dunnage_type"] = selectedDunnageTypeId,
                ["p_dunnage_part"] = selectedDunnagePartId,
                ["p_sub_parts"] = subordinatePartsJson,
                ["p_dunnage_parts"] = dunnagePartsJson,
            },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    [TestMethod]
    public async Task CoilOnlyJob_NormalisesToCoil()
    {
        var snapshot = await ResolveOwnedAsync(_coilOnly);

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual("MMC0001000", snapshot.Coils[0].PartNumber);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
        Assert.AreEqual(0, snapshot.Dies.Count);
    }

    [TestMethod]
    public async Task FlatstockOnlyJob_IsNormalisedByThePartNumberPrefix()
    {
        // The row is deliberately stored with Category 'Component'; the MMF prefix is authoritative, so the
        // real read path must land it in Flatstock. This is the job that proves a flatstock-only job is
        // offered pickup-coil (D21).
        var snapshot = await ResolveOwnedAsync(_flatstockOnly);

        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual("MMF0001154", snapshot.Flatstock[0].PartNumber);
        Assert.AreEqual(0, snapshot.Components.Count, "The MMF prefix must win over the stored Component tag.");
    }

    [TestMethod]
    public async Task DieOnlyJob_NormalisesToDie()
    {
        var snapshot = await ResolveOwnedAsync(_dieOnly);

        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task ComponentOnlyJob_KeepsTheStoredCategory()
    {
        var snapshot = await ResolveOwnedAsync(_componentOnly);

        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.AreEqual(0, snapshot.Coils.Count);
        Assert.AreEqual(0, snapshot.Flatstock.Count);
    }

    [TestMethod]
    public async Task DunnageOnlyJob_HasNoSubordinatePart()
    {
        var snapshot = await ResolveOwnedAsync(_dunnageOnly);

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0, "The dunnage-only job assigns a dunnage part.");
    }

    [TestMethod]
    public async Task EverythingAtOnceJob_NormalisesEveryCategory()
    {
        var snapshot = await ResolveOwnedAsync(_everythingAtOnce);

        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.IsTrue(snapshot.DunnageParts.Count > 0);
    }

    [TestMethod]
    public async Task EmptySubordinateArray_ResolvesWithNothingPresent()
    {
        var snapshot = await ResolveOwnedAsync(_noSubordinatePart);

        Assert.AreEqual(0, snapshot.SubordinateParts.Count);
        Assert.AreEqual(0, snapshot.DunnageParts.Count);
    }

    [TestMethod]
    public async Task WorkCentreWithNoRow_ReadsAsNoActiveJob()
    {
        // The eighth configuration is realised by the ABSENCE of a row, and "no active job" is what the
        // resolver must answer for it.
        var snapshot = await ResolveAsync(_workCentreWithNoActiveJob);

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
    public async Task ScrapValues_CoverTheWholeThreeWayGate()
    {
        // A real type (coil only), the placeholder (die only) and 'No Scrap' (component only) — the input
        // FR-031 is written against — must survive the round trip verbatim, because the picker's scrap gate
        // reads them.
        Assert.IsTrue(ScrapDecisionRules.HasRealScrapDecision((await ResolveOwnedAsync(_coilOnly)).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveOwnedAsync(_dieOnly)).SubordinateParts[0].SelectedScrapType));
        Assert.IsFalse(ScrapDecisionRules.HasRealScrapDecision((await ResolveOwnedAsync(_componentOnly)).SubordinateParts[0].SelectedScrapType));
    }

    private async Task<SetupActiveJobSnapshot> ResolveOwnedAsync(string workCenter)
    {
        var snapshot = await ResolveAsync(workCenter);
        Assert.IsNotNull(snapshot, $"The fixture row written for work centre '{workCenter}' must be readable through the resolver.");
        return snapshot!;
    }

    private Task<SetupActiveJobSnapshot?> ResolveAsync(string workCenter) =>
        _resolver!.ResolveAsync(workCenter);
}
