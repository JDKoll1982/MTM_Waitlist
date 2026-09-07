using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class UrgencyAllotmentEditorViewModelTests
{
    private static UrgencyAllotmentEditorViewModel Build(
        ILocalSettingsService settings,
        string role,
        IRequestTypeEditorService? catalog = null)
    {
        var startupState = new StartupState { CurrentRole = role };
        return new UrgencyAllotmentEditorViewModel(
            new UrgencySettingsService(settings),
            catalog ?? new EmptyCatalogService(),
            startupState);
    }

    [TestMethod]
    public void CanManage_IsTrueForPlantManagerAndAbove()
    {
        Assert.IsTrue(Build(new StubLocalSettings(), "Plant Manager").CanManageUrgencySettings);
        Assert.IsTrue(Build(new StubLocalSettings(), "Developer").CanManageUrgencySettings);
        Assert.IsTrue(Build(new StubLocalSettings(), "Admin").CanManageUrgencySettings);

        Assert.IsFalse(Build(new StubLocalSettings(), "Material Handler").CanManageUrgencySettings);
        Assert.IsFalse(Build(new StubLocalSettings(), "Production").CanManageUrgencySettings);
        Assert.IsFalse(Build(new StubLocalSettings(), "Setup Lead").CanManageUrgencySettings);
    }

    [TestMethod]
    public async Task Load_AddsSubtypeRowsWithStoredOrDefaultMinutes()
    {
        var settings = new StubLocalSettings();
        settings.Set(UrgencySettingsService.KeyPrefix + "Wrong Coil", 20); // stored override
        var vm = Build(settings, "Plant Manager", new TwoSubtypeCatalogService());

        await vm.LoadAsync();

        Assert.AreEqual(2, vm.Items.Count);
        Assert.AreEqual("Pickup Coil", vm.Items[0].SubtypeName);
        Assert.AreEqual(30, vm.Items[0].Minutes); // default
        Assert.AreEqual("Wrong Coil", vm.Items[1].SubtypeName);
        Assert.AreEqual(20, vm.Items[1].Minutes); // stored
    }

    [TestMethod]
    public async Task EditingMinutes_PersistsUnderSubtypeKey()
    {
        var settings = new StubLocalSettings();
        var vm = Build(settings, "Plant Manager", new TwoSubtypeCatalogService());
        await vm.LoadAsync();

        vm.Items[0].Minutes = 45;
        await Task.Delay(40);

        Assert.AreEqual(45, settings.Get(UrgencySettingsService.KeyPrefix + "Pickup Coil"));
    }

    [TestMethod]
    public async Task Load_EmptyCatalog_YieldsNoRowsAndStatus()
    {
        var settings = new StubLocalSettings();
        var vm = Build(settings, "Plant Manager", new EmptyCatalogService());

        await vm.LoadAsync();

        Assert.AreEqual(0, vm.Items.Count);
        StringAssert.Contains(vm.StatusMessage, "sub-type");
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new();

        public void Set(string key, int value) => _store[key] = value;

        public object? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (!_store.TryGetValue(key, out var raw) || raw is not T typed)
            {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult<T?>(typed);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }

    private sealed class EmptyCatalogService : IRequestTypeEditorService
    {
        public Task<IReadOnlyList<RequestTypeEditorItem>> GetCatalogAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RequestTypeEditorItem>>(Array.Empty<RequestTypeEditorItem>());

        public Task<int> AddSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> UpdateSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> DeleteSubtypeAsync(long subtypeId, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> UpdateTypeAsync(RequestTypeEditorItem type, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class TwoSubtypeCatalogService : IRequestTypeEditorService
    {
        public Task<IReadOnlyList<RequestTypeEditorItem>> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            var type = new RequestTypeEditorItem
            {
                Name = "Coil",
                Subtypes =
                {
                    new RequestSubtypeEditorItem { Name = "Pickup Coil" },
                    new RequestSubtypeEditorItem { Name = "Wrong Coil" },
                },
            };
            return Task.FromResult<IReadOnlyList<RequestTypeEditorItem>>(new[] { type });
        }

        public Task<int> AddSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> UpdateSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> DeleteSubtypeAsync(long subtypeId, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> UpdateTypeAsync(RequestTypeEditorItem type, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
