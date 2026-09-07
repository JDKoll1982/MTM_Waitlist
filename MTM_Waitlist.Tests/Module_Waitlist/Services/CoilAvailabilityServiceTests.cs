using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class CoilAvailabilityServiceTests
{
    [TestMethod]
    public async Task GetCoilForJobAsync_MockOn_WithCoilBearingJob_ReturnsCoil()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
        });
        var service = new CoilAvailabilityService(settings);

        var coil = await service.GetCoilForJobAsync("100-3");

        Assert.IsTrue(coil.HasCoil);
        Assert.AreEqual("COIL-204", coil.CoilNumber);
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.QuantityOnHand));
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.Description));
        Assert.IsFalse(string.IsNullOrWhiteSpace(coil.AverageWeight));
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_MockOn_WithNoCoilJob_ReturnsNoCoil()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
        });
        var service = new CoilAvailabilityService(settings);

        var coil = await service.GetCoilForJobAsync("100-17");

        Assert.IsFalse(coil.HasCoil);
        Assert.AreEqual(string.Empty, coil.CoilNumber);
    }

    [TestMethod]
    public async Task GetCoilForJobAsync_MockOff_KeepsCoilAvailable()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = false,
        });
        var service = new CoilAvailabilityService(settings);

        var coil = await service.GetCoilForJobAsync("100-17");

        Assert.IsTrue(coil.HasCoil);
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return Task.FromResult((T?)value);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
