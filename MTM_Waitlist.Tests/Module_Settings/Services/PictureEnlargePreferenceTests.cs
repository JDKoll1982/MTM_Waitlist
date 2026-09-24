using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The per-person enlarge-pictures preference (009): on unless the person turned it off, stored against their own
/// account, and never able to break a picture by failing to read or write (FR-030, FR-031).
/// </summary>
[TestClass]
public sealed class PictureEnlargePreferenceTests
{
    private const long SignedInUserId = 7;

    [TestMethod]
    public async Task LoadAsync_NothingStored_KeepsTheShippedDefaultOn()
    {
        var preference = Build(new FakeConfigSettingsValueService(), SignedInUserId);

        await preference.LoadAsync();

        Assert.IsTrue(preference.IsEnabled, "The declared default is on, so a person who has never chosen keeps it.");
    }

    [TestMethod]
    public async Task LoadAsync_StoredOff_AnswersOff()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetBool(ConfigSettingKeys.EnlargePicturesOnClick, false);

        var preference = Build(stored, SignedInUserId);

        await preference.LoadAsync();

        Assert.IsFalse(preference.IsEnabled, "A stored answer must be used rather than the default.");
    }

    [TestMethod]
    public async Task LoadAsync_StoreUnreachable_KeepsTheShippedDefaultOn()
    {
        var preference = Build(new UnreachableSettingsValueService(), SignedInUserId);

        await preference.LoadAsync();

        Assert.IsTrue(preference.IsEnabled, "A store that cannot be reached must not leave pictures un-enlargeable.");
    }

    /// <summary>
    /// The value belongs to the signed-in person, so it is written at their own scope and carries their id — the
    /// same shape the user list's saved filter uses, and the reason two people sharing a computer can disagree.
    /// </summary>
    [TestMethod]
    public async Task SetEnabledAsync_SignedInPerson_StoresAgainstThatAccount()
    {
        var stored = new FakeConfigSettingsValueService();
        var preference = Build(stored, SignedInUserId);

        await preference.SetEnabledAsync(false);

        var saved = stored.SavedValues.Single();

        Assert.AreEqual(ConfigSettingKeys.EnlargePicturesOnClick, saved.SettingKey);
        Assert.AreEqual("user", saved.ScopeType);
        Assert.AreEqual($"user:{SignedInUserId}", saved.ScopeKey);
        Assert.AreEqual(SignedInUserId, saved.UserId);
        Assert.AreEqual("bool", saved.ValueType);
        Assert.IsFalse(saved.SettingValueBool);
        Assert.IsFalse(preference.IsEnabled, "The change applies straight away, and not only after a restart.");
    }

    [TestMethod]
    public async Task SetEnabledAsync_StoreRefuses_StillAppliesForThisSession()
    {
        var preference = Build(new UnreachableSettingsValueService(), SignedInUserId);

        await preference.SetEnabledAsync(false);

        Assert.IsFalse(preference.IsEnabled, "A refused write must not undo the reader's choice on the open screen.");
    }

    /// <summary>
    /// Nobody signed in means no account to read for, so the read is not marked done: an operator who signs in
    /// afterwards still gets their stored answer rather than the default.
    /// </summary>
    [TestMethod]
    public async Task LoadAsync_NobodySignedIn_LeavesTheReadForLater()
    {
        var stored = new FakeConfigSettingsValueService();
        stored.SetBool(ConfigSettingKeys.EnlargePicturesOnClick, false);
        var state = new StartupState();
        var preference = new PictureEnlargePreference(stored, state);

        await preference.LoadAsync();

        Assert.IsTrue(preference.IsEnabled, "With no signed-in account there is nothing to read, so the default stands.");
        Assert.AreEqual(0, stored.GetCallCount, "No store read should be attempted without an account.");

        state.UserId = SignedInUserId;
        await preference.LoadAsync();

        Assert.IsFalse(preference.IsEnabled, "The read is still owed once a session exists.");
    }

    private static PictureEnlargePreference Build(IConfigSettingsValueService stored, long userId) =>
        new(stored, new StartupState { UserId = userId, Username = "JSMITH" });

    /// <summary>A store that refuses everything, which is what an unreachable database answers with.</summary>
    private sealed class UnreachableSettingsValueService : IConfigSettingsValueService
    {
        public Task<ConfigSettingValue?> GetSettingValueAsync(string settingKey, string scopeKey) =>
            throw new InvalidOperationException("The store is unreachable.");

        public Task SetSettingValueAsync(ConfigSettingValue setting, long? updatedByUserId = null) =>
            throw new InvalidOperationException("The store is unreachable.");

        public Task DeleteSettingValueAsync(string settingKey, string scopeKey) =>
            throw new InvalidOperationException("The store is unreachable.");
    }
}
