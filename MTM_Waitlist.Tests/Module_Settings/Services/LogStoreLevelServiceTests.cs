using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The one reader and writer of the level the log store keeps, which is a plant-wide setting rather than anything
/// a person or a computer captures (FR-032, FR-040).
/// </summary>
[TestClass]
public sealed class LogStoreLevelServiceTests
{
    /// <summary>Nothing stored means the shipped default is in force, and it must never read as "keep nothing".</summary>
    [TestMethod]
    public async Task Service_LogStoreLevelServiceTests_WithNothingStored_TheShippedDefaultIsInForce()
    {
        var settings = new FakeConfigSettingsValueService();
        var level = new LogStoreLevel(LogSeverity.Debug);

        var service = new LogStoreLevelService(settings, level);

        var applied = await service.ApplyStoredLevelAsync();

        Assert.AreEqual(LogStoreLevel.ShippedMinimum, applied);
        Assert.AreEqual(LogStoreLevel.ShippedMinimum, level.MinimumLevel, "the store must be narrowed to the default, not left wide");
    }

    /// <summary>A stored level is read and adopted, so the launch keeps what the plant decided.</summary>
    [TestMethod]
    public async Task Service_LogStoreLevelServiceTests_AStoredLevel_IsReadAndAdopted()
    {
        var settings = new FakeConfigSettingsValueService();
        settings.SetText(ConfigSettingKeys.LogStoreMinimumLevel, "info");

        var level = new LogStoreLevel();
        var service = new LogStoreLevelService(settings, level);

        var applied = await service.ApplyStoredLevelAsync();

        Assert.AreEqual(LogSeverity.Info, applied, "a stored severity name must be read case-insensitively");
        Assert.IsTrue(level.Keeps(LogSeverity.Info), "the stored level was not adopted");
    }

    /// <summary>A level the store cannot read answers the default and never the widest option.</summary>
    [TestMethod]
    public async Task Service_LogStoreLevelServiceTests_AStoredLevelThatNamesNoSeverity_AnswersTheDefault()
    {
        var settings = new FakeConfigSettingsValueService();
        settings.SetText(ConfigSettingKeys.LogStoreMinimumLevel, "chatty");

        var service = new LogStoreLevelService(settings, new LogStoreLevel());

        Assert.AreEqual(LogStoreLevel.ShippedMinimum, await service.ReadStoredLevelAsync());
    }

    /// <summary>
    /// Saving writes one plant-wide row and adopts it on the same object the seam reads, which is what makes the
    /// change apply without a restart.
    /// </summary>
    [TestMethod]
    public async Task Service_LogStoreLevelServiceTests_SavingALevel_WritesOnePlantWideRowAndAdoptsIt()
    {
        var settings = new FakeConfigSettingsValueService();
        var level = new LogStoreLevel();
        var service = new LogStoreLevelService(settings, level);

        await service.SaveLevelAsync(LogSeverity.Error, actorUserId: 7);

        var written = settings.SavedValues.Single();

        Assert.AreEqual(ConfigSettingKeys.LogStoreMinimumLevel, written.SettingKey);
        Assert.AreEqual("all_users", written.ScopeType, "the level is the plant's decision, not one computer's");
        Assert.AreEqual("all_users", written.ScopeKey);
        Assert.AreEqual("text", written.ValueType);
        Assert.AreEqual("Error", written.SettingValue);

        Assert.IsFalse(level.Keeps(LogSeverity.Warning), "the saved level was not adopted");
        Assert.IsTrue(level.Keeps(LogSeverity.Error));
    }
}
