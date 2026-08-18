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
        StringAssert.Contains(insert.Sql, "INSERT INTO config_images_locations");
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
        StringAssert.Contains(statement.Sql, "UPDATE config_images_locations");
        StringAssert.Contains(statement.Sql, "is_active = 1");
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
        StringAssert.Contains(statement.Sql, "UPDATE config_images_locations");
        Assert.AreEqual("new.png", statement.Parameters["p_image_path"]);
    }

    [TestMethod]
    public async Task DeleteOverrideAsync_WhenRowIsDeactivated_ReportsSuccess()
    {
        _helper.EnqueueNonQueryResult(1);

        var result = await _service.DeleteOverrideAsync("work_center", "42");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        var statement = _helper.ExecutedNonQueries.Single();
        StringAssert.Contains(statement.Sql, "is_active = 0");
    }

    [TestMethod]
    public async Task DeleteOverrideAsync_WhenNothingWasDeactivated_ReturnsNotFound()
    {
        _helper.EnqueueNonQueryResult(0);

        var result = await _service.DeleteOverrideAsync("work_center", "42");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_FOUND", result.ErrorCode);
    }
}
