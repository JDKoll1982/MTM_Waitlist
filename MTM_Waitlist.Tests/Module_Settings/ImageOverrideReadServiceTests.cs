using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Exercises the config_images_locations read path against a scripted helper server.
/// </summary>
[TestClass]
public sealed class ImageOverrideReadServiceTests
{
    private FakeMySqlHelperServer _helper = null!;
    private ImageOverrideReadService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _helper = new FakeMySqlHelperServer();
        _service = new ImageOverrideReadService(_helper, NullLogger<ImageOverrideReadService>.Instance);
    }

    [TestMethod]
    public async Task GetOverrideAsync_WhenRowExists_MapsEveryColumn()
    {
        _helper.EnqueueQueryResult(FakeMySqlHelperServer.OverrideRow(
            "request_type", "abc-123", @"\\server\images\rt.png", id: 7, publicId: "11111111-1111-1111-1111-111111111111"));

        var result = await _service.GetOverrideAsync("request_type", "abc-123");

        Assert.IsNotNull(result);
        Assert.AreEqual(7, result!.RecordId);
        Assert.AreEqual("11111111-1111-1111-1111-111111111111", result.PublicId);
        Assert.AreEqual("request_type", result.Scope);
        Assert.AreEqual("abc-123", result.ScopeItemId);
        Assert.AreEqual(@"\\server\images\rt.png", result.ImagePath);
        Assert.IsTrue(result.IsActive);
    }

    /// <summary>
    /// The scope and active-flag predicates live in the procedure body now (constitution III), so what the service
    /// owes the database is the procedure name plus the two keys. The predicates themselves are asserted against
    /// the artifact and the live server, not here.
    /// </summary>
    [TestMethod]
    public async Task GetOverrideAsync_AsksTheProcedureForTheScopeItemKey()
    {
        _helper.EnqueueEmptyQueryResult();

        await _service.GetOverrideAsync("work_center", "42");

        var executed = _helper.ExecutedQueries.Single();
        Assert.AreEqual("sp_config_images_locations_get", executed.Sql);
        Assert.AreEqual("work_center", executed.Parameters["p_scope"]);
        Assert.AreEqual("42", executed.Parameters["p_scope_item_id"]);
    }

    [TestMethod]
    public async Task GetOverrideAsync_WhenNoRow_ReturnsNull()
    {
        _helper.EnqueueEmptyQueryResult();

        Assert.IsNull(await _service.GetOverrideAsync("request_type", "abc-123"));
    }

    [DataTestMethod]
    [DataRow("request_type")]
    [DataRow("request_subtype")]
    [DataRow("work_center")]
    public async Task GetOverrideAsync_AcceptsEveryValidScope(string scope)
    {
        _helper.EnqueueEmptyQueryResult();

        await _service.GetOverrideAsync(scope, "1");

        Assert.AreEqual(1, _helper.ExecutedQueries.Count);
    }

    [TestMethod]
    public async Task GetOverrideAsync_WithUnknownScope_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.GetOverrideAsync("building", "1"));
    }

    [TestMethod]
    public async Task GetOverrideAsync_WithEmptyScopeItemId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _service.GetOverrideAsync("request_type", " "));
    }

    [TestMethod]
    public async Task GetOverridesByScopeAsync_ReturnsEveryRow()
    {
        _helper.EnqueueQueryResult(
            FakeMySqlHelperServer.OverrideRow("work_center", "1", "a.png", id: 1),
            FakeMySqlHelperServer.OverrideRow("work_center", "2", "b.png", id: 2));

        var results = await _service.GetOverridesByScopeAsync("work_center");

        Assert.AreEqual(2, results.Count);
        CollectionAssert.AreEquivalent(new[] { "1", "2" }, results.Select(r => r.ScopeItemId).ToArray());
    }

    [TestMethod]
    public async Task HasOverrideAsync_ReflectsWhetherARowWasReturned()
    {
        _helper.EnqueueQueryResult(FakeMySqlHelperServer.OverrideRow("request_type", "abc", "a.png"));
        Assert.IsTrue(await _service.HasOverrideAsync("request_type", "abc"));

        _helper.EnqueueEmptyQueryResult();
        Assert.IsFalse(await _service.HasOverrideAsync("request_type", "abc"));
    }

    /// <summary>
    /// Constitution III: the read service must not carry statement text. Every call it makes is a
    /// stored-procedure invocation, and each one names the artifact that holds the statement.
    /// </summary>
    [TestMethod]
    public async Task EveryRead_RoutesThroughItsProcedure_AndCarriesNoInlineStatement()
    {
        _helper.EnqueueEmptyQueryResult();
        await _service.GetOverrideAsync("work_center", "1");

        _helper.EnqueueEmptyQueryResult();
        await _service.GetOverridesByScopeAsync("work_center");

        _helper.EnqueueQueryResult(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["count"] = 0 });
        await _service.CountAllActiveOverridesAsync();

        _helper.EnqueueQueryResult(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["count"] = 0 });
        await _service.CountActiveOverridesByScopeAsync("work_center");

        _helper.EnqueueEmptyQueryResult();
        await _service.DetectOrphanedOverridesAsync();

        _helper.EnqueueEmptyQueryResult();
        await _service.GetOverrideByPublicIdAsync("11111111-1111-1111-1111-111111111111");

        _helper.EnqueueEmptyQueryResult();
        await _service.GetRecentlyUpdatedOverridesAsync(10);

        var statements = _helper.ExecutedQueries.Select(executed => executed.Sql).ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "sp_config_images_locations_get",
                "sp_config_images_locations_get_by_scope",
                "sp_config_images_locations_count_active_get",
                "sp_config_images_locations_count_by_scope_get",
                "sp_config_images_locations_get_all",
                "sp_config_images_locations_get_by_public_id",
                "sp_config_images_locations_recent_get"
            },
            statements);

        Assert.IsFalse(
            statements.Any(statement => statement.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
                || statement.Contains("FROM ", StringComparison.OrdinalIgnoreCase)),
            "A read path is still carrying inline SQL instead of naming a procedure.");
    }

    /// <summary>
    /// The record cap used to be interpolated straight into the statement text (`LIMIT {maxRecordCount}`), which
    /// is the reason this one read needed a procedure at all. It is a bound parameter now.
    /// </summary>
    [TestMethod]
    public async Task GetRecentlyUpdatedOverridesAsync_BindsTheCap_InsteadOfInterpolatingIt()
    {
        _helper.EnqueueEmptyQueryResult();

        await _service.GetRecentlyUpdatedOverridesAsync(25);

        var executed = _helper.ExecutedQueries.Single();
        Assert.AreEqual("sp_config_images_locations_recent_get", executed.Sql);
        Assert.AreEqual(25, executed.Parameters["p_max_rows"]);
    }

    /// <summary>
    /// The orphan path is the only reader that reaches the work-center existence check, so it is where that
    /// procedure has to be proven wired.
    /// </summary>
    [TestMethod]
    public async Task DetectOrphanedOverridesAsync_ChecksWorkCenterExistenceThroughItsProcedure()
    {
        _helper.EnqueueQueryResult(FakeMySqlHelperServer.OverrideRow("work_center", "1", "a.png"));
        _helper.EnqueueEmptyQueryResult();

        var orphans = await _service.DetectOrphanedOverridesAsync();

        Assert.AreEqual(1, orphans.Count);
        CollectionAssert.AreEqual(
            new[] { "sp_config_images_locations_get_all", "sp_setup_work_centers_exists_get" },
            _helper.ExecutedQueries.Select(executed => executed.Sql).ToList());
        Assert.AreEqual(1L, _helper.ExecutedQueries[1].Parameters["p_id"]);
    }
}
