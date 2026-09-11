using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies durable, atomic configuration persistence and the FR-026 credential rules:
/// the credential is stored only as a DPAPI-protected blob, is never persisted in plaintext,
/// and is compared in constant time.
/// </summary>
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
    public async Task LoadAsync_OnFirstRun_GeneratesACredential()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);

        Assert.IsFalse(store.HasCredential, "A brand-new store must not report a credential before loading.");

        await store.LoadAsync();

        Assert.IsTrue(store.HasCredential, "Loading on first run must generate a credential.");
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
    public async Task SaveAsync_NeverPersistsTheCredentialInPlaintext()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        var plaintext = await store.GenerateCredentialAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(plaintext), "Generating a credential must yield a value for provisioning.");

        var fileContents = await File.ReadAllTextAsync(store.ConfigurationFilePath);

        Assert.IsFalse(
            fileContents.Contains(plaintext, StringComparison.Ordinal),
            "FR-026: the configuration file must never contain the plaintext credential.");
    }

    [TestMethod]
    public async Task CredentialMatches_AcceptsTheGeneratedValue_AndRejectsEverythingElse()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        var plaintext = await store.GenerateCredentialAsync();

        Assert.IsTrue(store.CredentialMatches(plaintext), "The generated credential must match itself.");
        Assert.IsFalse(store.CredentialMatches(plaintext + "x"), "A longer token must not match.");
        Assert.IsFalse(store.CredentialMatches(plaintext[..^1]), "A truncated token must not match.");
        Assert.IsFalse(store.CredentialMatches(null), "A missing token must not match.");
        Assert.IsFalse(store.CredentialMatches(string.Empty), "An empty token must not match.");
    }

    [TestMethod]
    public async Task RotatingTheCredential_InvalidatesThePreviousValue()
    {
        var store = new ServiceConfigurationStore(_appDataRoot);
        await store.LoadAsync();

        var original = await store.GenerateCredentialAsync();
        var rotated = await store.GenerateCredentialAsync();

        Assert.AreNotEqual(original, rotated, "Rotation must produce a new value.");
        Assert.IsFalse(store.CredentialMatches(original), "Rotation must invalidate the previous credential.");
        Assert.IsTrue(store.CredentialMatches(rotated), "The rotated credential must be the one in force.");
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
