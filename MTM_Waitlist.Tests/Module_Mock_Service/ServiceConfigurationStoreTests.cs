using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies durable, atomic configuration persistence.
/// </summary>
/// <remarks>
/// The credential this type used to own is gone (T147): the API is authorized by the caller's application
/// role, so the store must hold no secret at all. The assertions below pin that — a configuration file that
/// gained a password, key or token field would be a regression.
/// </remarks>
[TestClass]
public sealed class ServiceConfigurationStoreTests
{
    private string _appDataRoot = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _appDataRoot = Path.Combine(Path.GetTempPath(), "mtm-mock-service-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_appDataRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_appDataRoot))
        {
            Directory.Delete(_appDataRoot, recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadAsync_OnFirstRun_PersistsTheDefaultConfiguration()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);

        await store.LoadAsync();

        Assert.IsTrue(File.Exists(store.ConfigurationFilePath), "First run must persist the configuration file.");
    }

    [TestMethod]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsOperatorSettings()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        var updated = store.Current with
        {
            RefreshInterval = TimeSpan.FromMinutes(42),
            AutoStartAtLogon = false,
            MysqldumpPath = null
        };

        await store.SaveAsync(updated);

        var reloaded = await new ServiceConfigurationStore(_appDataRoot).LoadAsync();

        Assert.AreEqual(TimeSpan.FromMinutes(42), reloaded.RefreshInterval);
        Assert.IsFalse(reloaded.AutoStartAtLogon);
    }

    [TestMethod]
    public async Task SaveAsync_IsAtomic_AndLeavesNoTemporaryFile()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        await store.SaveAsync(store.Current);

        var leftovers = Directory.GetFiles(_appDataRoot, "*.tmp");
        Assert.AreEqual(0, leftovers.Length, "An atomic save must not leave a temporary file behind.");
    }

    [TestMethod]
    public async Task SaveAsync_NeverPersistsASecret()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        await store.SaveAsync(store.Current);

        var fileContents = await File.ReadAllTextAsync(store.ConfigurationFilePath);

        Assert.IsFalse(
            fileContents.Contains("credential", StringComparison.OrdinalIgnoreCase),
            "T147: there is no credential any more, so none may be persisted.");
        Assert.IsFalse(
            fileContents.Contains("password", StringComparison.OrdinalIgnoreCase),
            "The configuration file must never carry a password field.");
    }

    [TestMethod]
    public void ApiSettings_ExposesNoCredentialProperty()
    {
        Assert.IsNull(
            typeof(ApiSettings).GetProperty("Credential"),
            "T147 removed the shared credential, so the API settings must not carry one.");
    }

    [TestMethod]
    public void Validate_RejectsAnOutOfRangePort()
    {
        var configuration = ServiceConfiguration.CreateDefault(_appDataRoot) with
        {
            Api = new ApiSettings { Port = 70000 }
        };

        Assert.ThrowsException<ArgumentException>(() => ServiceConfigurationStore.Validate(configuration));
    }

    [TestMethod]
    public void Validate_RejectsAMissingPerStoreBackupPolicy()
    {
        // The four-store set is fixed; dropping one is a configuration error, not an extension point.
        var configuration = ServiceConfiguration.CreateDefault(_appDataRoot) with
        {
            BackupPolicies = new Dictionary<BackupStore, BackupPolicy>()
        };

        Assert.ThrowsException<ArgumentException>(() => ServiceConfigurationStore.Validate(configuration));
    }

    [TestMethod]
    public void Validate_RejectsANonPositiveRetentionCount()
    {
        var defaults = ServiceConfiguration.CreateDefault(_appDataRoot);
        var policies = defaults.BackupPolicies.ToDictionary(
            pair => pair.Key,
            pair => pair.Key == BackupStore.MtmMock
                ? pair.Value with { RetentionCount = 0 }
                : pair.Value);

        Assert.ThrowsException<ArgumentException>(
            () => ServiceConfigurationStore.Validate(defaults with { BackupPolicies = policies }));
    }

    [TestMethod]
    public void CreateDefault_ProvidesOneEnabledPolicyPerStore()
    {
        var configuration = ServiceConfiguration.CreateDefault(_appDataRoot);

        Assert.AreEqual(4, configuration.BackupPolicies.Count);
        foreach (var store in BackupStoreExtensions.All)
        {
            Assert.IsTrue(configuration.BackupPolicies.ContainsKey(store), $"Missing policy for {store}.");
            Assert.IsTrue(configuration.BackupPolicies[store].IsEnabled, $"{store} must default to enabled.");
        }

        // The shipped default is the eight-slot local schedule: every 3 hours from local midnight
        // (00:00/03:00/06:00/09:00/12:00/15:00/18:00/21:00 server-local).
        Assert.AreEqual(TimeSpan.FromHours(3), configuration.RefreshInterval);
        Assert.AreEqual(5760, configuration.Api.Port);
        Assert.IsTrue(configuration.AutoStartAtLogon);
    }
}
