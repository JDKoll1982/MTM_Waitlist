using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class NewRequestAlertServiceTests
{
    [TestMethod]
    public async Task Service_NewRequestAlertServiceTests_GetEnabled_DefaultsOffWhenNeverSet()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var service = new NewRequestAlertService(preferences);

        Assert.IsFalse(await service.GetEnabledAsync());
        CollectionAssert.Contains(
            preferences.Reads,
            (NewRequestAlertService.SettingKeyName, PreferenceScope.Person),
            "A person's alert switch is read from the person's own scope, which is what makes it follow them.");
    }

    [TestMethod]
    public async Task Service_NewRequestAlertServiceTests_GetEnabled_ReturnsStoredValue()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedFlag(NewRequestAlertService.SettingKeyName, PreferenceScope.Person, true);
        var service = new NewRequestAlertService(preferences);

        Assert.IsTrue(await service.GetEnabledAsync());
    }

    [TestMethod]
    public async Task Service_NewRequestAlertServiceTests_Set_PersistsUnderSettingKey()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var service = new NewRequestAlertService(preferences);

        await service.SetEnabledAsync(true);

        Assert.IsTrue(preferences.FlagFor(NewRequestAlertService.SettingKeyName, PreferenceScope.Person));
    }

    [TestMethod]
    public async Task Service_NewRequestAlertServiceTests_ShouldNotifyOnCreated_RequiresSignalEnabledAndPackaged()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedFlag(NewRequestAlertService.SettingKeyName, PreferenceScope.Person, true);
        var service = new NewRequestAlertService(preferences);

        Assert.IsTrue(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: true));
        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: false, isPackaged: true));
        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: false));
    }

    [TestMethod]
    public async Task Service_NewRequestAlertServiceTests_ShouldNotifyOnCreated_RespectsOffToggle()
    {
        // Never set -> defaults OFF, so even when a request is created in a packaged app, no toast.
        var preferences = new InMemoryScopedPreferenceStore();
        var service = new NewRequestAlertService(preferences);

        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: true));
    }
}
