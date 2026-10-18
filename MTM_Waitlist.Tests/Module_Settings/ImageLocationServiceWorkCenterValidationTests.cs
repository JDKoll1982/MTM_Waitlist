using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The image service's work-center validation used to accept every positive number and leave the real check for a
/// later phase. These cases pin it to the catalog read the image-override path already validates against, so an
/// ID the catalog does not hold is no longer reported as a work center.
/// </summary>
[TestClass]
public sealed class ImageLocationServiceWorkCenterValidationTests
{
    private FakeMySqlHelperServer _mysql = null!;

    [TestInitialize]
    public void TestInitialize() => _mysql = new FakeMySqlHelperServer();

    private ImageLocationService CreateService(IMySqlHelperServer? mySqlHelperServer = null) =>
        new(
            NullLogger<ImageLocationService>.Instance,
            new FakeImageOverrideReadService(),
            new FakeImageStorageConfigurationResolver(),
            new FakeWorkCenterCatalogService(),
            mySqlHelperServer ?? _mysql);

    [TestMethod]
    public async Task Service_ImageLocationServiceWorkCenterValidationTests_IsValidWorkCenterIdAsync_ARowFromTheCatalogMeansTheIdIsAWorkCenter()
    {
        var service = CreateService();
        await service.InitializeAsync();
        _mysql.EnqueueQueryResult(new Dictionary<string, object?> { ["id"] = 42L });

        var valid = await service.IsValidWorkCenterIdAsync(42);

        Assert.IsTrue(valid, "An ID the catalog returns a row for is a work center.");
        service.Dispose();
    }

    [TestMethod]
    public async Task Service_ImageLocationServiceWorkCenterValidationTests_IsValidWorkCenterIdAsync_NoRowFromTheCatalogMeansTheIdIsUnknown()
    {
        var service = CreateService();
        await service.InitializeAsync();
        _mysql.EnqueueEmptyQueryResult();

        var valid = await service.IsValidWorkCenterIdAsync(9_999_999);

        Assert.IsFalse(valid, "An ID the catalog does not hold is not a work center.");
        service.Dispose();
    }

    [TestMethod]
    public async Task Service_ImageLocationServiceWorkCenterValidationTests_IsValidWorkCenterIdAsync_ANonPositiveIdIsRefusedWithoutReadingTheCatalog()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var valid = await service.IsValidWorkCenterIdAsync(0);

        Assert.IsFalse(valid, "Zero cannot name a work center.");
        Assert.AreEqual(
            0,
            _mysql.ExecutedQueries.Count,
            "A number that cannot be an ID is answered without asking the database.");
        service.Dispose();
    }

    [TestMethod]
    public async Task Service_ImageLocationServiceWorkCenterValidationTests_IsValidWorkCenterIdAsync_BeforeInitializationTheServiceRefuses()
    {
        var service = CreateService();

        var valid = await service.IsValidWorkCenterIdAsync(42);

        Assert.IsFalse(valid, "A service that has not read its configuration does not claim to validate anything.");
        service.Dispose();
    }

    [TestMethod]
    public async Task Service_ImageLocationServiceWorkCenterValidationTests_IsValidWorkCenterIdAsync_ACatalogThatCannotBeReadIsReportedAsUnknown()
    {
        var service = CreateService(new FailingMySqlHelperServer());
        await service.InitializeAsync();

        var valid = await service.IsValidWorkCenterIdAsync(42);

        Assert.IsFalse(
            valid,
            "A failed read says nothing about the ID, so the caller is told it is not known rather than being "
            + "left to report the work center as deleted.");
        service.Dispose();
    }

    /// <summary>
    /// A store that cannot be reached, so the validation's failure path can be observed without a live database.
    /// </summary>
    private sealed class FailingMySqlHelperServer : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(
                new InvalidOperationException("The work center catalog is unreachable."));

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromException<int>(new InvalidOperationException("The work center catalog is unreachable."));

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(
                new InvalidOperationException("The work center catalog is unreachable."));

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromException<int>(new InvalidOperationException("The work center catalog is unreachable."));
    }
}
