using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class SettingsViewModelIgnoredLocationsTests
{
    [TestMethod]
    public void DefaultSeed_PopulatesTheDefaultIgnoredLocations()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Production Lead");

        CollectionAssert.AreEquivalent(
            new[] { "WC", "NCM", "V-WC", "NCM-VITS", "SHIP" },
            viewModel.IgnoredLocations.ToArray());
    }

    [TestMethod]
    public async Task AddAndRemove_PersistToLocalSettings()
    {
        var settings = new RecordingLocalSettingsService();
        var viewModel = BuildViewModel(settings, "Production Lead");

        // Add (auto-uppercased, dedupes) then remove.
        viewModel.IgnoredLocationInput = "wc";
        viewModel.AddIgnoredLocationCommand.Execute(null);
        await Task.Delay(30);
        Assert.IsTrue(viewModel.IgnoredLocations.Contains("WC"), "Lowercase entry should be added as uppercase.");

        viewModel.RemoveIgnoredLocationCommand.Execute("WC");
        await Task.Delay(30);
        Assert.IsFalse(viewModel.IgnoredLocations.Contains("WC"), "Removed location should be gone.");

        var stored = settings.ReadSettingAsync<List<string>?>("Feature.IgnoredLocations").GetAwaiter().GetResult();
        Assert.IsNotNull(stored);
        Assert.IsFalse(stored!.Contains("WC", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(stored.Contains("NCM", StringComparer.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PersistenceRoundTrip_SeedsFromStoredInsteadOfDefaults()
    {
        var settings = new RecordingLocalSettingsService();
        settings.SaveSettingAsync("Feature.IgnoredLocations", new List<string> { "WC", "SCRAP-1" }).GetAwaiter().GetResult();

        var viewModel = BuildViewModel(settings, "Production Lead");

        CollectionAssert.AreEquivalent(new[] { "WC", "SCRAP-1" }, viewModel.IgnoredLocations.ToArray());
    }

    [TestMethod]
    public void AddRejectsInvalidCode_WhenNotAllowedToManage()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Material Handler");

        // Role gate: Material Handler cannot edit.
        Assert.IsFalse(viewModel.CanManageIgnoredLocations);

        viewModel.IgnoredLocationInput = "SCRAP-2";
        viewModel.AddIgnoredLocationCommand.Execute(null);
        Assert.IsFalse(viewModel.IgnoredLocations.Contains("SCRAP-2"));
    }

    [TestMethod]
    public void RoleGating_AllowsRolesAboveMaterialHandler()
    {
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Material Handler").CanManageIgnoredLocations);
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Operator").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Production").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Production Lead").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Setup").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Setup Lead").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Plant Manager").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Admin").CanManageIgnoredLocations);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Developer").CanManageIgnoredLocations);
    }

    [TestMethod]
    public void LocationCodeValidation_AcceptsCodePatternAndRejectsInvalid()
    {
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("WC"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("NCM"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("V-WC"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("NCM-VITS"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("SHIP"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("A1-B2"));

        Assert.IsFalse(SettingsViewModel.IsValidLocationCode(string.Empty));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("-WC"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("WC-"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("A B"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("A@B"));
    }

    [TestMethod]
    public void NewRequestAlerts_DefaultsOff()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Production");
        Assert.IsFalse(viewModel.NewRequestAlertsEnabled);
    }

    [TestMethod]
    public void NewRequestAlerts_PanelAndCategoryMatchSearch()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Production");
        Assert.IsTrue(viewModel.IsNewRequestAlertsPanelVisible, "Panel visible with no search query.");

        viewModel.SearchQuery = "notification";
        Assert.IsTrue(viewModel.IsNewRequestAlertsPanelVisible, "Panel should match 'notification'.");
        Assert.IsTrue(viewModel.IsOperationsCategoryVisible, "Operations category should include the alerts panel.");
    }

    [TestMethod]
    public async Task NewRequestAlerts_TogglePersistsUnderKey()
    {
        var settings = new RecordingLocalSettingsService();
        var viewModel = BuildViewModel(settings, "Production");

        viewModel.NewRequestAlertsEnabled = true;
        await Task.Delay(30);

        var stored = settings.ReadSettingAsync<bool?>(NewRequestAlertService.SettingKeyName).GetAwaiter().GetResult();
        Assert.IsTrue(stored == true, "Toggling on should persist true under the alert key.");
    }

    [TestMethod]
    public void CanRequestCacheRefresh_IsRestrictedToPlantManagerAndAbove()
    {
        // The gate is a strict subset of the service's approved operator roles, so it must exclude both the
        // leads and the shop-floor roles, and include the three the Max Allotted Time panel already gates on.
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Material Handler").CanRequestCacheRefresh);
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Production").CanRequestCacheRefresh);
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Production Lead").CanRequestCacheRefresh);
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Setup").CanRequestCacheRefresh);
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Setup Lead").CanRequestCacheRefresh);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Plant Manager").CanRequestCacheRefresh);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Admin").CanRequestCacheRefresh);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Developer").CanRequestCacheRefresh);

        // The panel is "access only": a role outside the gate must not even see the surface.
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService(), "Setup Lead").IsCacheRefreshPanelVisible);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "Plant Manager").IsCacheRefreshPanelVisible);
    }

    [TestMethod]
    public void CanRequestCacheRefresh_MatchesTheRoleCaseInsensitively()
    {
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "plant manager").CanRequestCacheRefresh);
        Assert.IsTrue(BuildViewModel(new RecordingLocalSettingsService(), "ADMIN").CanRequestCacheRefresh);
    }

    [TestMethod]
    public async Task RequestCacheRefresh_DoesNotReachTheService_WhenTheRoleIsNotAllowed()
    {
        var client = new FakeMockServiceRefreshClient();
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Setup Lead", client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual(0, client.CallCount, "A role outside the gate must never reach the service.");
        Assert.AreEqual(string.Empty, viewModel.CacheRefreshStatusMessage);
    }

    [TestMethod]
    public async Task RequestCacheRefresh_ReportsTheShapesThatRefreshed()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = new RefreshRequestResult
            {
                Succeeded = true,
                ShapeOutcomes = new Dictionary<string, string>
                {
                    ["work_order_lookup"] = "refreshed",
                    ["inventory_locations"] = "refreshed",
                },
            },
        };
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Plant Manager", client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual(1, client.CallCount);
        Assert.IsNull(client.LastShapeKeys, "The panel asks for every enabled shape, not a subset.");
        StringAssert.Contains(viewModel.CacheRefreshStatusMessage, "2 read shape(s) refreshed");
        Assert.IsFalse(viewModel.IsCacheRefreshing, "The busy indicator must clear when the request finishes.");
    }

    [TestMethod]
    public async Task RequestCacheRefresh_NamesAShapeThatCouldNotRefresh()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = new RefreshRequestResult
            {
                Succeeded = true,
                ShapeOutcomes = new Dictionary<string, string>
                {
                    ["work_order_lookup"] = "refreshed",
                    ["inventory_locations"] = "skippedSourceUnreachable",
                },
            },
        };
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Developer", client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        StringAssert.Contains(viewModel.CacheRefreshStatusMessage, "inventory_locations: skippedSourceUnreachable");
    }

    [TestMethod]
    public async Task RequestCacheRefresh_ReportsAnUnconfiguredOrAbsentService_WithoutThrowing()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = RefreshRequestResult.Unavailable("No endpoint is installed."),
        };
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), "Admin", client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual("No endpoint is installed.", viewModel.CacheRefreshStatusMessage);
        Assert.IsFalse(viewModel.IsCacheRefreshing);
    }

    private static SettingsViewModel BuildViewModel(
        RecordingLocalSettingsService settings,
        string role,
        IMockServiceRefreshClient? refreshClient = null)
    {
        var startupState = new StartupState { CurrentRole = role };
        var computerManagement = new ComputerManagementViewModel(new FakeComputerRegistryService(), startupState);
        var urgencyAllotments = new UrgencyAllotmentEditorViewModel(
            new UrgencySettingsService(settings),
            new FakeRequestSubtypeNameReadService(),
            startupState);
        return new SettingsViewModel(
            new FakeThemeSelectorService(),
            settings,
            new FakeWorkCenterCatalogService(),
            new FakeDunnageTypeVisibilityCatalogService(),
            new NewRequestAlertService(settings),
            startupState,
            computerManagement,
            urgencyAllotments,
            refreshClient ?? new FakeMockServiceRefreshClient());
    }

    /// <summary>
    /// Stands in for the on-host refresh client so the panel's command can be driven without a service.
    /// </summary>
    private sealed class FakeMockServiceRefreshClient : IMockServiceRefreshClient
    {
        public int CallCount { get; private set; }

        public IReadOnlyCollection<string>? LastShapeKeys { get; private set; }

        public RefreshRequestResult Result { get; init; } =
            new() { Succeeded = true, ShapeOutcomes = new Dictionary<string, string>() };

        public Task<RefreshRequestResult> RequestRefreshAsync(
            IReadOnlyCollection<string>? shapeKeys,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastShapeKeys = shapeKeys;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeRequestSubtypeNameReadService : IRequestSubtypeNameReadService
    {
        public Task<IReadOnlyList<string>> GetSubtypeNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class FakeThemeSelectorService : IThemeSelectorService
    {
        public ElementTheme Theme { get; set; } = ElementTheme.Default;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SetThemeAsync(ElementTheme theme)
        {
            Theme = theme;
            return Task.CompletedTask;
        }

        public Task SetRequestedThemeAsync() => Task.CompletedTask;
    }

    private sealed class FakeDunnageTypeVisibilityCatalogService : IDunnageTypeVisibilityCatalogService
    {
        public Task<DunnageTypeVisibilityCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DunnageTypeVisibilityCatalogResult());

        public Task<IReadOnlyDictionary<string, bool>> GetVisibilityMapAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, bool>>(new Dictionary<string, bool>());

        public Task<string?> SaveVisibleDunnageTypesAsync(IReadOnlyCollection<string> visibleDunnageTypeIds, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeComputerRegistryService : IComputerRegistryService
    {
        public Task<ComputerRecord?> LookupComputerAsync(string computerName, string macAddressNormalized, CancellationToken cancellationToken = default)
            => Task.FromResult<ComputerRecord?>(null);

        public Task<ComputerRecord?> LookupComputerByMacAsync(string macAddressNormalized, CancellationToken cancellationToken = default)
            => Task.FromResult<ComputerRecord?>(null);

        public Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ComputerRecord>>(Array.Empty<ComputerRecord>());

        public Task<ComputerRecord> UpsertComputerAsync(string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<ComputerRecord> UpdateComputerByMacAsync(string macAddressNormalized, string newComputerName, string hostnameNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<ComputerRecord> UpdateComputerAsync(long id, string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, bool isRegistered, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _values.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
