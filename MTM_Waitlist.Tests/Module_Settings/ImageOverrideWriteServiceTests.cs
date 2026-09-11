using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Exercises the config_images_locations write path, including the behaviour required by the
/// uq_config_images_locations_scope_item unique key.
/// </summary>
[TestClass]
public sealed class ImageOverrideWriteServiceTests
{
    private FakeMySqlHelperServer _helper = null!;
    private FakeImageOverrideReadService _readService = null!;
    private ImageLocationService _imageLocationService = null!;
    private ImageOverrideWriteService _service = null!;

    [TestCleanup]
    public void TestCleanup()
    {
        _imageLocationService?.Dispose();
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        _helper = new FakeMySqlHelperServer();
        _readService = new FakeImageOverrideReadService();

        _imageLocationService = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            new FakeRequestSubtypeDisplayLabelService(),
            _readService,
            new FakeImageStorageConfigurationResolver(),
            new FakeWorkCenterCatalogService(),
            _helper);

        await _imageLocationService.InitializeAsync();

        _service = new ImageOverrideWriteService(
            _helper,
            _readService,
            _imageLocationService,
            NullLogger<ImageOverrideWriteService>.Instance);
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WhenNoRowExists_InsertsAndReportsSuccess()
    {
        _helper.EnqueueEmptyQueryResult(); // existence probe
        _helper.EnqueueNonQueryResult(1);  // insert

        var result = await _service.CreateOverrideAsync("request_type", "abc-123", @"\\server\images\rt.png");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.AreEqual("CREATE", result.OperationType);

        var insert = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_insert", insert.Sql);
        Assert.AreEqual("request_type", insert.Parameters["p_scope"]);
        Assert.AreEqual("abc-123", insert.Parameters["p_scope_item_id"]);
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WhenActiveRowExists_ReturnsDuplicateKeyAndDoesNotInsert()
    {
        _helper.EnqueueQueryResult(FakeMySqlHelperServer.OverrideRow("request_type", "abc-123", "existing.png"));

        var result = await _service.CreateOverrideAsync("request_type", "abc-123", "new.png");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("DUPLICATE_KEY", result.ErrorCode);
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count, "A duplicate must never reach the INSERT.");
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WhenSoftDeletedRowExists_ReactivatesInsteadOfInserting()
    {
        // The unique key spans (scope, scope_item_id) regardless of is_active, so a second
        // INSERT for a soft-deleted pair would violate it.
        _helper.EnqueueQueryResult(FakeMySqlHelperServer.OverrideRow("work_center", "42", "old.png", isActive: false));
        _helper.EnqueueNonQueryResult(1);

        var result = await _service.CreateOverrideAsync("work_center", "42", "new.png");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        var statement = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_reactivate", statement.Sql);
        Assert.AreEqual("new.png", statement.Parameters["p_image_path"]);
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WhenInsertAffectsNoRows_ReportsDatabaseError()
    {
        _helper.EnqueueEmptyQueryResult();
        _helper.EnqueueNonQueryResult(0);

        var result = await _service.CreateOverrideAsync("request_type", "abc-123", "rt.png");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("DATABASE_ERROR", result.ErrorCode);
    }

    [TestMethod]
    public async Task CreateOverrideAsync_NotifiesSubscribersOnSuccess()
    {
        _helper.EnqueueEmptyQueryResult();
        _helper.EnqueueNonQueryResult(1);

        var notifications = 0;
        using var subscription = _imageLocationService.SubscribeToImageLocationChanges(_ => notifications++);

        await _service.CreateOverrideAsync("request_type", "abc-123", "rt.png");

        Assert.AreEqual(1, notifications);
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WhenPathExceedsColumnLength_Throws()
    {
        var tooLong = new string('x', 501);

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.CreateOverrideAsync("request_type", "abc-123", tooLong));
    }

    [TestMethod]
    public async Task CreateOverrideAsync_WithUnknownScope_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.CreateOverrideAsync("building", "abc-123", "rt.png"));
    }

    [TestMethod]
    public async Task UpdateOverrideAsync_WhenNoRowExists_ReturnsNotFound()
    {
        var result = await _service.UpdateOverrideAsync("request_type", "missing", "rt.png");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_FOUND", result.ErrorCode);
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count);
    }

    [TestMethod]
    public async Task UpdateOverrideAsync_WhenRowExists_IssuesAnUpdate()
    {
        _readService.AddOverride("request_type", "abc-123", "old.png");
        _helper.EnqueueNonQueryResult(1);

        var result = await _service.UpdateOverrideAsync("request_type", "abc-123", "new.png");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        var statement = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_update", statement.Sql);
        Assert.AreEqual("new.png", statement.Parameters["p_image_path"]);
    }

    [TestMethod]
    public async Task DeleteOverrideAsync_WhenRowIsDeactivated_ReportsSuccess()
    {
        _helper.EnqueueNonQueryResult(1);

        var result = await _service.DeleteOverrideAsync("work_center", "42");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        var statement = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_delete", statement.Sql);
        Assert.AreEqual("42", statement.Parameters["p_scope_item_id"]);
    }

    [TestMethod]
    public async Task DeleteOverrideAsync_WhenNothingWasDeactivated_ReturnsNotFound()
    {
        _helper.EnqueueNonQueryResult(0);

        var result = await _service.DeleteOverrideAsync("work_center", "42");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_FOUND", result.ErrorCode);
    }

    /// <summary>
    /// Regression: this method used to run its UPDATE through the row-returning helper and decide success from
    /// `rows.Count > 0`. An UPDATE returns no rows, so the count was always 0 and the method answered NOT_FOUND
    /// for every call — including the ones that did withdraw the row. The affected-row count is the only correct
    /// signal, and it is what the non-query path returns.
    /// </summary>
    [TestMethod]
    public async Task DeleteByPublicIdAsync_WhenTheRowWasWithdrawn_ReportsSuccess()
    {
        _helper.EnqueueNonQueryResult(1);

        var result = await _service.DeleteByPublicIdAsync("11111111-1111-1111-1111-111111111111");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        var statement = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_delete_by_public_id", statement.Sql);
        Assert.AreEqual("11111111-1111-1111-1111-111111111111", statement.Parameters["p_public_id"]);
    }

    [TestMethod]
    public async Task DeleteByPublicIdAsync_WhenNothingChanged_ReturnsNotFound()
    {
        _helper.EnqueueNonQueryResult(0);

        var result = await _service.DeleteByPublicIdAsync("11111111-1111-1111-1111-111111111111");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_FOUND", result.ErrorCode);
    }

    /// <summary>
    /// Regression: the purge count used to be read from the row-returning helper, which reports 0 for a DELETE,
    /// so callers were told nothing had been purged however many rows were removed.
    /// </summary>
    [TestMethod]
    public async Task PurgeInactiveOverridesAsync_ReturnsTheAffectedRowCount()
    {
        _helper.EnqueueNonQueryResult(3);

        var purged = await _service.PurgeInactiveOverridesAsync();

        Assert.AreEqual(3, purged);
        Assert.AreEqual("sp_config_images_locations_purge_inactive", _helper.ExecutedNonQueries.Single().Sql);
    }

    /// <summary>
    /// Regression: same defect as the purge, on the per-scope bulk withdraw — the UI reported "0 deactivated"
    /// however many overrides were withdrawn.
    /// </summary>
    [TestMethod]
    public async Task DeactivateAllForScopeAsync_ReturnsTheAffectedRowCount()
    {
        _helper.EnqueueNonQueryResult(2);

        var deactivated = await _service.DeactivateAllForScopeAsync("request_subtype");

        Assert.AreEqual(2, deactivated);
        var statement = _helper.ExecutedNonQueries.Single();
        Assert.AreEqual("sp_config_images_locations_deactivate_for_scope", statement.Sql);
        Assert.AreEqual("request_subtype", statement.Parameters["p_scope"]);
    }

    /// <summary>
    /// Constitution III: the write service must not carry statement text. Each of its paths is a stored-procedure
    /// invocation, and the query path is used only where a row comes back (the existence probe).
    /// </summary>
    [TestMethod]
    public async Task EveryWrite_RoutesThroughItsProcedure_AndCarriesNoInlineStatement()
    {
        _helper.EnqueueEmptyQueryResult(); // existence probe
        _helper.EnqueueNonQueryResult(1);  // insert
        await _service.CreateOverrideAsync("request_type", "abc-123", "new.png");

        _readService.AddOverride("request_type", "abc-123", "old.png");
        _helper.EnqueueNonQueryResult(1);
        await _service.UpdateOverrideAsync("request_type", "abc-123", "newer.png");

        _helper.EnqueueNonQueryResult(1);
        await _service.DeleteOverrideAsync("request_type", "abc-123");

        _helper.EnqueueNonQueryResult(1);
        await _service.DeleteByPublicIdAsync("11111111-1111-1111-1111-111111111111");

        _helper.EnqueueNonQueryResult(0);
        await _service.PurgeInactiveOverridesAsync();

        _helper.EnqueueNonQueryResult(0);
        await _service.DeactivateAllForScopeAsync("work_center");

        var statements = _helper.ExecutedNonQueries.Select(executed => executed.Sql)
            .Concat(_helper.ExecutedQueries.Select(executed => executed.Sql))
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "sp_config_images_locations_insert",
                "sp_config_images_locations_update",
                "sp_config_images_locations_delete",
                "sp_config_images_locations_delete_by_public_id",
                "sp_config_images_locations_purge_inactive",
                "sp_config_images_locations_deactivate_for_scope",
                "sp_config_images_locations_status_get"
            },
            statements);

        Assert.IsFalse(
            statements.Any(statement => statement.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase)
                || statement.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase)
                || statement.Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase)
                || statement.Contains("SELECT ", StringComparison.OrdinalIgnoreCase)),
            "A write path is still carrying inline SQL instead of naming a procedure.");
    }
}
