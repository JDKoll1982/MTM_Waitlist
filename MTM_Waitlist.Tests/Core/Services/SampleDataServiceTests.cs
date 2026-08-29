using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class SampleDataServiceTests
{
    [TestMethod]
    public void GetSampleOrders_ReturnsEmptyByDefaultWhenVisualMockDataIsUnset()
    {
        var service = new SampleDataService();

        var rows = service.GetSampleOrders();

        Assert.AreEqual(0, rows.Count);
    }

    [TestMethod]
    public void GetSampleOrders_ReturnsSixOrdersWhenVisualMockDataIsEnabled()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
        });

        var service = new SampleDataService(settings);

        var rows = service.GetSampleOrders();

        Assert.AreEqual(6, rows.Count);
        var firstOrder = rows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;
        Assert.IsNotNull(firstOrder);
        Assert.AreEqual("Coil Request", firstOrder!.Title);
        Assert.AreEqual("Jordan Lee", firstOrder.RequestedByName);
        Assert.AreEqual("Coil Request 100-01", firstOrder.RequestedPressName);
        Assert.AreEqual("00:27", firstOrder.RemainingTimeText);
    }

    [TestMethod]
    public void GetSampleOrders_UsesDifferentDataForEachBuilding()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
        });

        var service = new SampleDataService(settings);

        var expoRows = service.GetSampleOrders("Expo Drive");
        var vitsRows = service.GetSampleOrders("Vits Drive");

        var expoOrder = expoRows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;
        var vitsOrder = vitsRows[0] as MTM_Waitlist.Module_Waitlist.Models.SampleOrder;

        Assert.IsNotNull(expoOrder);
        Assert.IsNotNull(vitsOrder);
        Assert.AreNotEqual(expoOrder!.Title, vitsOrder!.Title);
        Assert.AreNotEqual(expoOrder.RequestedByName, vitsOrder!.RequestedByName);
        Assert.AreEqual(6, expoRows.Count);
        Assert.AreEqual(6, vitsRows.Count);

        var uniqueImagePaths = expoRows.Concat(vitsRows)
            .OfType<MTM_Waitlist.Module_Waitlist.Models.SampleOrder>()
            .Select(item => item.ImagePath)
            .Distinct()
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "coil.png",
                "pickup_fg.png",
                "pickup_ncm.png",
                "pickup_os.png",
                "pickup_wip.png",
                "scrap.png"
            },
            uniqueImagePaths);
    }

    [TestMethod]
    public async Task GetSampleOrders_ReturnsEmptyWhenMockDataDisabledAsync()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = false,
            ["Feature.RecvMockData"] = false,
        });

        var service = new SampleDataService(settings);

        var rows = await Task.FromResult(service.GetSampleOrders());

        Assert.AreEqual(0, rows.Count);
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