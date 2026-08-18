using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Round-trips real rows through config_images_locations to prove the SQL and the
/// uq_config_images_locations_scope_item unique key behave as the services assume.
///
/// Requires MTM_WAITLIST_TEST_DB_CONNECTION_STRING to point at a schema-provisioned MySQL instance.
/// Without it every test reports inconclusive rather than failing, so the suite stays green offline.
///
/// A dedicated variable is used on purpose: MTM_WAITLIST_DB_CONNECTION_STRING changes connection
/// resolution for the whole process, which would break the Module_Setup tests that assert
/// no-backend-configured behaviour. The connection string is passed straight to the helper instead.
/// </summary>
[TestClass]
public sealed class ConfigImagesLocationsIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private MySqlHelperServer _helper = null!;
    private ImageOverrideReadService _readService = null!;
    private ImageOverrideWriteService _writeService = null!;
    private ImageLocationService _imageLocationService = null!;
    private string _scopeItemId = string.Empty;

    [TestInitialize]
    public async Task TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        _helper = new MySqlHelperServer(
            new FakeLocalSettingsService(),
            new FakeSampleDataService(),
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
        _readService = new ImageOverrideReadService(_helper, NullLogger<ImageOverrideReadService>.Instance);

        _imageLocationService = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            new FakeRequestSubtypeDisplayLabelService(),
            _readService,
            new FakeImageStorageConfigurationResolver(),
            new FakeWorkCenterCatalogService(),
            _helper);

        await _imageLocationService.InitializeAsync();

        _writeService = new ImageOverrideWriteService(
            _helper,
            _readService,
            _imageLocationService,
            NullLogger<ImageOverrideWriteService>.Instance);

        // Namespaced per test run so parallel or interrupted runs never collide with real data.
        _scopeItemId = $"itest-{Guid.NewGuid():N}";

        await AssertDatabaseReachableAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        _imageLocationService?.Dispose();

        if (_helper is null || string.IsNullOrEmpty(_scopeItemId))
        {
            return;
        }

        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE FROM config_images_locations WHERE scope_item_id = @p_scope_item_id;",
            new Dictionary<string, object?> { ["p_scope_item_id"] = _scopeItemId },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    private async Task AssertDatabaseReachableAsync()
    {
        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) AS c FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'config_images_locations';",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        if (rows.Count == 0 || Convert.ToInt32(rows[0]["c"]) != 1)
        {
            Assert.Inconclusive("config_images_locations is not present on the configured database; skipping.");
        }
    }

    [TestMethod]
    public async Task CreateThenRead_RoundTripsTheOverride()
    {
        var created = await _writeService.CreateOverrideAsync("request_type", _scopeItemId, @"\\share\images\rt.png");
        Assert.IsTrue(created.Success, created.ErrorMessage);

        var loaded = await _readService.GetOverrideAsync("request_type", _scopeItemId);

        Assert.IsNotNull(loaded);
        Assert.AreEqual("request_type", loaded!.Scope);
        Assert.AreEqual(_scopeItemId, loaded.ScopeItemId);
        Assert.AreEqual(@"\\share\images\rt.png", loaded.ImagePath);
        Assert.IsTrue(loaded.IsActive);
        Assert.IsTrue(Guid.TryParse(loaded.PublicId, out _), "public_id must be a GUID.");
    }

    [TestMethod]
    public async Task CreateTwice_IsRejectedByTheUniqueScopeItemKey()
    {
        var first = await _writeService.CreateOverrideAsync("request_type", _scopeItemId, "first.png");
        Assert.IsTrue(first.Success, first.ErrorMessage);

        var second = await _writeService.CreateOverrideAsync("request_type", _scopeItemId, "second.png");

        Assert.IsFalse(second.Success);
        Assert.AreEqual("DUPLICATE_KEY", second.ErrorCode);

        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) AS c FROM config_images_locations WHERE scope = 'request_type' AND scope_item_id = @p_scope_item_id;",
            new Dictionary<string, object?> { ["p_scope_item_id"] = _scopeItemId },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, Convert.ToInt32(rows[0]["c"]), "The unique key must keep exactly one row per scope/item pair.");
    }

    [TestMethod]
    public async Task TheSameScopeItemIdIsAllowedUnderADifferentScope()
    {
        var asRequestType = await _writeService.CreateOverrideAsync("request_type", _scopeItemId, "rt.png");
        var asSubtype = await _writeService.CreateOverrideAsync("request_subtype", _scopeItemId, "st.png");

        Assert.IsTrue(asRequestType.Success, asRequestType.ErrorMessage);
        Assert.IsTrue(asSubtype.Success, asSubtype.ErrorMessage);

        Assert.AreEqual("rt.png", (await _readService.GetOverrideAsync("request_type", _scopeItemId))!.ImagePath);
        Assert.AreEqual("st.png", (await _readService.GetOverrideAsync("request_subtype", _scopeItemId))!.ImagePath);
    }

    [TestMethod]
    public async Task UpdateOverride_PersistsTheNewPath()
    {
        await _writeService.CreateOverrideAsync("work_center", _scopeItemId, "old.png");

        var updated = await _writeService.UpdateOverrideAsync("work_center", _scopeItemId, "new.png");
        Assert.IsTrue(updated.Success, updated.ErrorMessage);

        var loaded = await _readService.GetOverrideAsync("work_center", _scopeItemId);
        Assert.AreEqual("new.png", loaded!.ImagePath);
    }

    [TestMethod]
    public async Task DeleteOverride_SoftDeletesSoTheReadPathStopsReturningIt()
    {
        await _writeService.CreateOverrideAsync("work_center", _scopeItemId, "wc.png");

        var deleted = await _writeService.DeleteOverrideAsync("work_center", _scopeItemId);
        Assert.IsTrue(deleted.Success, deleted.ErrorMessage);

        Assert.IsNull(await _readService.GetOverrideAsync("work_center", _scopeItemId));

        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT is_active FROM config_images_locations WHERE scope = 'work_center' AND scope_item_id = @p_scope_item_id;",
            new Dictionary<string, object?> { ["p_scope_item_id"] = _scopeItemId },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, rows.Count, "Delete must be a soft delete; the row has to survive.");
        Assert.AreEqual(0, Convert.ToInt32(rows[0]["is_active"]));
    }

    [TestMethod]
    public async Task CreateAfterDelete_ReactivatesTheExistingRow()
    {
        await _writeService.CreateOverrideAsync("work_center", _scopeItemId, "first.png");
        await _writeService.DeleteOverrideAsync("work_center", _scopeItemId);

        var recreated = await _writeService.CreateOverrideAsync("work_center", _scopeItemId, "second.png");

        Assert.IsTrue(recreated.Success, recreated.ErrorMessage);

        var loaded = await _readService.GetOverrideAsync("work_center", _scopeItemId);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("second.png", loaded!.ImagePath);

        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) AS c FROM config_images_locations WHERE scope = 'work_center' AND scope_item_id = @p_scope_item_id;",
            new Dictionary<string, object?> { ["p_scope_item_id"] = _scopeItemId },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, Convert.ToInt32(rows[0]["c"]), "Reactivation must not create a second row.");
    }

    [TestMethod]
    public async Task DeleteOverride_WhenNothingExists_ReturnsNotFound()
    {
        var result = await _writeService.DeleteOverrideAsync("work_center", _scopeItemId);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_FOUND", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetOverridesByScopeAsync_IncludesTheCreatedRow()
    {
        await _writeService.CreateOverrideAsync("request_subtype", _scopeItemId, "st.png");

        var all = await _readService.GetOverridesByScopeAsync("request_subtype");

        Assert.IsTrue(all.Any(o => o.ScopeItemId == _scopeItemId));
    }
}
