using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Tests.Module_Settings;

using WinUIEx;

namespace MTM_Waitlist.Tests.Services;

/// <summary>
/// T138, US6: a person's preferences are held against the person in the store, so signing in on another computer
/// finds them already there (FR-023, SC-008).
/// </summary>
/// <remarks>
/// "Another computer" is a second service built over the same store, which is exactly what the claim means: a
/// preference that lived in one computer's settings file would be gone, and one held against the person comes back.
/// Every case also asserts the scope the value was written at, because a preference written at plant scope would
/// come back as well and would be the wrong answer.
/// </remarks>
[TestClass]
public sealed class ScopedPreferenceTests
{
    [TestMethod]
    public async Task TheChosenTheme_ComesFromTheStoreAtPersonScope()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        preferences.SeedText(ThemeSelectorService.SettingsKey, PreferenceScope.Person, ElementTheme.Dark.ToString());

        // A second computer: a fresh service over the same store.
        var service = new ThemeSelectorService(preferences, new NeverTouchedWindowProvider());
        await service.InitializeAsync();

        Assert.AreEqual(ElementTheme.Dark, service.Theme, "The theme travels with the person rather than the computer.");
        CollectionAssert.Contains(
            preferences.Reads,
            (ThemeSelectorService.SettingsKey, PreferenceScope.Person),
            "The theme is read from the person's own scope, which is what makes it follow them.");
    }

    [TestMethod]
    public async Task TheWaitlistOrder_IsStoredAgainstThePersonAndComesBackOnAnotherComputer()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        await new WaitlistSortPreferenceService(preferences).SetSortOrderAsync(WaitlistSortOrder.RequestedBy);

        var onTheOtherComputer = await new WaitlistSortPreferenceService(preferences).GetSortOrderAsync();

        Assert.AreEqual(WaitlistSortOrder.RequestedBy, onTheOtherComputer);
        CollectionAssert.Contains(
            preferences.Writes,
            (WaitlistSortPreferenceService.SettingsKey, PreferenceScope.Person));
    }

    [TestMethod]
    public async Task TheNewRequestAlertSwitch_IsStoredAgainstThePersonAndComesBackOnAnotherComputer()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        await new NewRequestAlertService(preferences).SetEnabledAsync(true);

        var onTheOtherComputer = await new NewRequestAlertService(preferences).GetEnabledAsync();

        Assert.IsTrue(onTheOtherComputer, "A person who wants alerts keeps them wherever they sign in.");
        CollectionAssert.Contains(
            preferences.Writes,
            (NewRequestAlertService.SettingKeyName, PreferenceScope.Person));
    }

    [TestMethod]
    public async Task TheRequestsAlreadySeen_AreStoredAgainstThePersonAndComeBackOnAnotherComputer()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var requestId = Guid.NewGuid();
        var seenAt = new DateTimeOffset(2026, 9, 27, 14, 30, 0, TimeSpan.Zero);

        await new LocalWaitlistMessageSeenStore(preferences).MarkSeenAsync(requestId, seenAt);

        var onTheOtherComputer = await new LocalWaitlistMessageSeenStore(preferences).GetLastSeenUtcAsync(requestId);

        Assert.AreEqual(seenAt, onTheOtherComputer, "A request read on one computer stays read on the next.");
        CollectionAssert.Contains(
            preferences.Writes,
            (LocalWaitlistMessageSeenStore.SettingsKey, PreferenceScope.Person));
    }

    [TestMethod]
    public async Task ThePartsWithoutPicturesChoice_IsStoredAgainstThePersonAndComesBackOnAnotherComputer()
    {
        var preferences = new InMemoryScopedPreferenceStore();

        var onThisComputer = new SetupDunnageImageSearchDialogViewModel(new EmptyDunnageWorkflowService(), preferences);
        await onThisComputer.InitializeAsync();
        await onThisComputer.HandleShowPartsWithoutImagesChangedAsync(false);

        Assert.AreEqual(
            false,
            preferences.FlagFor(SetupDunnageImageSearchDialogViewModel.SettingsKey, PreferenceScope.Person),
            "Narrowing the list to parts with pictures is stored against the person.");

        var onTheOtherComputer = new SetupDunnageImageSearchDialogViewModel(new EmptyDunnageWorkflowService(), preferences);
        await onTheOtherComputer.InitializeAsync();

        Assert.IsFalse(onTheOtherComputer.ShowPartsWithoutImages, "The choice travels with the person.");
    }

    /// <summary>
    /// No preference in these five cases may be written at plant scope: one written there would follow the plant
    /// instead of the person, which is the fault this story removes.
    /// </summary>
    [TestMethod]
    public async Task NoPersonPreferenceIsWrittenAtPlantScope()
    {
        var preferences = new InMemoryScopedPreferenceStore();

        await new WaitlistSortPreferenceService(preferences).SetSortOrderAsync(WaitlistSortOrder.Press);
        await new NewRequestAlertService(preferences).SetEnabledAsync(false);
        await new LocalWaitlistMessageSeenStore(preferences).MarkSeenAsync(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var plantScoped = preferences.Writes.Where(write => write.Scope is PreferenceScope.Plant).ToArray();

        Assert.AreEqual(
            0,
            plantScoped.Length,
            $"A person's choice belongs to that person. Plant-scoped writes seen: {string.Join(", ", plantScoped.Select(write => write.Key))}.");
    }

    /// <summary>
    /// A provider that throws if the window is ever asked for it. The theme read is the whole case here, and it
    /// must not need a window: a preference that could not be read without one would break the launch.
    /// </summary>
    private sealed class NeverTouchedWindowProvider : IAppWindowProvider
    {
        public WindowEx MainWindow =>
            throw new InvalidOperationException("Reading a stored theme must not need the main window.");
    }

    /// <summary>A dunnage catalogue with nothing in it, which is all the preference case needs.</summary>
    private sealed class EmptyDunnageWorkflowService : IDunnageWorkflowService
    {
        public Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesAsync(
            string partNumber,
            string sequenceNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupDunnageType>>(Array.Empty<SetupDunnageType>());

        public Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsAsync(
            string dunnageTypeId,
            string partNumber,
            string sequenceNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupDunnagePart>>(Array.Empty<SetupDunnagePart>());

        public Task<IReadOnlyList<SetupDunnagePart>> GetAllDunnagePartsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupDunnagePart>>(Array.Empty<SetupDunnagePart>());

        public Task<SetupSelectionResult> AddDunnageTypeAsync(string typeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult());

        public Task<SetupSelectionResult> AddDunnagePartAsync(string dunnageTypeId, string partName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult());
    }
}
