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

    [TestMethod]
    public async Task GetOverrideAsync_FiltersOnScopeItemAndActiveFlag()
    {
        _helper.EnqueueEmptyQueryResult();

        await _service.GetOverrideAsync("work_center", "42");

        var executed = _helper.ExecutedQueries.Single();
        StringAssert.Contains(executed.Sql, "FROM config_images_locations");
        StringAssert.Contains(executed.Sql, "is_active = 1");
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
}
