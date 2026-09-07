using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class UrgencyDeadlineServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    private static UrgencyDeadlineService Build(StubLocalSettings settings)
        => new(new UrgencySettingsService(settings));

    [TestMethod]
    public async Task Compute_DueIsCreatedPlusDefaultAllotted()
    {
        var settings = new StubLocalSettings();
        var service = Build(settings);

        var created = Now.AddMinutes(-10);
        var state = await service.ComputeAsync(created, "Pickup Coil", Now);

        Assert.AreEqual(created + TimeSpan.FromMinutes(30), state.DueUtc);
        Assert.AreEqual(TimeSpan.FromMinutes(20), state.Remaining);
        Assert.IsFalse(state.IsOverdue);
    }

    [TestMethod]
    public async Task Compute_UsesStoredSubtypeOverride()
    {
        var settings = new StubLocalSettings();
        settings.Set(UrgencySettingsService.KeyPrefix + "Wrong Coil", 60);
        var service = Build(settings);

        var created = Now.AddMinutes(-70);
        var state = await service.ComputeAsync(created, "Wrong Coil", Now);

        Assert.AreEqual(created + TimeSpan.FromMinutes(60), state.DueUtc);
        Assert.IsTrue(state.IsOverdue);
    }

    [TestMethod]
    public async Task GetMaxAllotted_ReturnsStoredOrDefault()
    {
        var settings = new StubLocalSettings();
        settings.Set(UrgencySettingsService.KeyPrefix + "Pickup", 45);
        var service = Build(settings);

        Assert.AreEqual(TimeSpan.FromMinutes(45), await service.GetMaxAllottedAsync("Pickup"));
        Assert.AreEqual(TimeSpan.FromMinutes(30), await service.GetMaxAllottedAsync("Unconfigured"));
        Assert.AreEqual(TimeSpan.FromMinutes(30), await service.GetMaxAllottedAsync(null));
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new();

        public void Set(string key, int value) => _store[key] = value;

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
}
