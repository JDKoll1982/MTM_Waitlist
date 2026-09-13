using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// US5 (FR-017, FR-018, FR-019, FR-020, FR-026; SC-007, SC-008). The minutes screen is keyed by <b>Item</b>, and
/// it shows the Item's configured minutes and the average its completed requests actually took as <b>two
/// clearly separate values</b> — the first editable, the second read-only display data that is never written
/// back as though it were configured.
/// </summary>
/// <remarks>
/// The screen keeps its <b>own</b> role gate, <see cref="UrgencyAllotmentEditorViewModel.CanManageUrgencySettings"/>.
/// FR-020's "the role gate" is two gates today — this one and the picture screen's
/// <c>CanManageImageLocationSettings</c> — and this class proves the minutes editor still uses its own.
/// </remarks>
[TestClass]
public sealed class UrgencyAllotmentEditorViewModelTests
{
    private const string ConfiguredItem = "pickup-coil";
    private const string UnconfiguredItem = "deliver-coil";

    private static UrgencyAllotmentEditorViewModel Build(
        FakeRequestItemAllottedMinutesStore store,
        string role,
        FakeRequestItemObservedTimeService? observedTimes = null)
    {
        var startupState = new StartupState { CurrentRole = role };
        return new UrgencyAllotmentEditorViewModel(
            new UrgencySettingsService(store),
            observedTimes ?? DefaultObservedTimes(),
            startupState);
    }

    /// <summary>Two rows: one Item with a configured figure and a real average, one with neither.</summary>
    private static FakeRequestItemObservedTimeService DefaultObservedTimes()
    {
        var service = new FakeRequestItemObservedTimeService();
        service.Add(ConfiguredItem, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(80));
        service.Add(UnconfiguredItem, null, null);
        return service;
    }

    // ── The role gate that already governs this screen (§D4, FR-020) ─────────────────────────────────────

    [TestMethod]
    public void CanManage_IsTrueForPlantManagerAndAbove()
    {
        Assert.IsTrue(Build(new FakeRequestItemAllottedMinutesStore(), "Plant Manager").CanManageUrgencySettings);
        Assert.IsTrue(Build(new FakeRequestItemAllottedMinutesStore(), "Developer").CanManageUrgencySettings);
        Assert.IsTrue(Build(new FakeRequestItemAllottedMinutesStore(), "Admin").CanManageUrgencySettings);

        Assert.IsFalse(Build(new FakeRequestItemAllottedMinutesStore(), "Material Handler").CanManageUrgencySettings);
        Assert.IsFalse(Build(new FakeRequestItemAllottedMinutesStore(), "Production").CanManageUrgencySettings);
        Assert.IsFalse(Build(new FakeRequestItemAllottedMinutesStore(), "Setup Lead").CanManageUrgencySettings);
    }

    // ── Configured and observed are two values, shown side by side (FR-018, SC-008) ──────────────────────

    [TestMethod]
    public async Task Load_ShowsTheConfiguredMinutesAndTheObservedAverageAsTwoSeparateValues()
    {
        var viewModel = Build(new FakeRequestItemAllottedMinutesStore(), "Plant Manager");

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Items.Count);

        var configured = viewModel.Items.Single(row => row.ItemCode == ConfiguredItem);
        Assert.AreEqual(30, configured.Minutes, "The editable column is the CONFIGURED figure.");
        Assert.AreEqual(
            TimeSpan.FromMinutes(80),
            configured.ObservedAverage,
            "The read-only column is the OBSERVED average, and it differs from the configured figure.");
        Assert.IsTrue(configured.HasObservedAverage);
        Assert.AreNotEqual(
            configured.Minutes,
            configured.ObservedAverage!.Value.TotalMinutes,
            "A screen that showed one number twice would pass every other check in this class.");
    }

    [TestMethod]
    public async Task Load_ForAnItemWithNoConfiguredMinutes_UsesThe15MinuteDefaultAndLabelsItAsADefault()
    {
        var viewModel = Build(new FakeRequestItemAllottedMinutesStore(), "Plant Manager");

        await viewModel.LoadAsync();

        var configured = viewModel.Items.Single(row => row.ItemCode == ConfiguredItem);
        var unconfigured = viewModel.Items.Single(row => row.ItemCode == UnconfiguredItem);

        Assert.AreEqual(15, unconfigured.Minutes, "FR-017's fallback is 15 minutes, labelled as a default.");
        Assert.IsTrue(unconfigured.IsConfiguredValueDefault, "A row measured by the fallback must say so.");
        Assert.IsFalse(configured.IsConfiguredValueDefault, "A row someone configured is not a default.");
        Assert.AreNotEqual(
            configured.ConfiguredValueLabel,
            unconfigured.ConfiguredValueLabel,
            "The default is labelled as a default rather than as configured.");
        Assert.AreEqual(15, UrgencySettingsService.DefaultMinutes);
    }

    [TestMethod]
    public async Task Load_ForAnItemWithNoCompletedRequest_ShowsNoObservedValueRatherThanAZero()
    {
        var viewModel = Build(new FakeRequestItemAllottedMinutesStore(), "Plant Manager");

        await viewModel.LoadAsync();

        var unconfigured = viewModel.Items.Single(row => row.ItemCode == UnconfiguredItem);

        Assert.IsFalse(unconfigured.HasObservedAverage);
        Assert.IsNull(unconfigured.ObservedAverage, "No value, not zero minutes (FR-026).");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(unconfigured.ObservedAverageText),
            "The absence is stated in words rather than rendered blank.");
    }

    // ── The observed value is never written back (FR-019) ───────────────────────────────────────────────

    [TestMethod]
    public async Task Load_NeverWritesTheObservedAverageBackAsAConfiguredValue()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        var viewModel = Build(store, "Plant Manager");

        await viewModel.LoadAsync();

        Assert.AreEqual(
            0,
            store.Writes.Count,
            "Opening the screen is a read. Writing the observed 80 minutes back as 'configured' is exactly the conflation FR-019 forbids.");
    }

    // ── The write path: the Item's stored minutes, clamped (FR-016, §D4) ────────────────────────────────

    [TestMethod]
    public async Task EditingMinutes_WritesTheItemsStoredAllotment()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        var viewModel = Build(store, "Plant Manager");
        await viewModel.LoadAsync();

        viewModel.Items.Single(row => row.ItemCode == ConfiguredItem).Minutes = 45;
        await WaitForWriteAsync(store);

        Assert.AreEqual(1, store.Writes.Count);
        Assert.AreEqual(ConfiguredItem, store.Writes[0].Item, "The write is keyed by Item, not by a subtype.");
        Assert.AreEqual(45, store.Writes[0].Minutes);
    }

    [TestMethod]
    public async Task EditingMinutes_IsClampedToThePositiveMinimumAndTheTwentyFourHourMaximum()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        var viewModel = Build(store, "Plant Manager");
        await viewModel.LoadAsync();

        var row = viewModel.Items.Single(item => item.ItemCode == ConfiguredItem);

        row.Minutes = 0;
        await WaitForWriteAsync(store);

        row.Minutes = 5000;
        await WaitForWriteAsync(store, expectedWrites: 2);

        Assert.AreEqual(2, store.Writes.Count);
        Assert.AreEqual(1, store.Writes[0].Minutes, "The same positive minimum UrgencySettingsService has always clamped to.");
        Assert.AreEqual(24 * 60, store.Writes[1].Minutes, "The same twenty-four-hour maximum.");
    }

    [TestMethod]
    public async Task EditingMinutes_IsRefusedForAViewerWithoutTheScreensOwnGate()
    {
        var store = new FakeRequestItemAllottedMinutesStore();
        var viewModel = Build(store, "Material Handler");
        await viewModel.LoadAsync();

        viewModel.Items.Single(row => row.ItemCode == ConfiguredItem).Minutes = 45;
        await Task.Delay(60);

        Assert.AreEqual(
            0,
            store.Writes.Count,
            "A viewer who cannot manage urgency settings may not write an Item's allotment (FR-020).");
    }

    // ── A failed read is reported, never turned into a fabricated row (FR-026) ─────────────────────────

    [TestMethod]
    public async Task Load_WhenThePairsCannotBeRead_ReportsItInPlainLanguage()
    {
        var observedTimes = new FakeRequestItemObservedTimeService { Failure = new InvalidOperationException("store down") };
        var viewModel = Build(new FakeRequestItemAllottedMinutesStore(), "Plant Manager", observedTimes);

        await viewModel.LoadAsync();

        Assert.AreEqual(0, viewModel.Items.Count, "Nothing is invented to fill the screen.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.StatusMessage), "The failure is reported.");
        Assert.IsFalse(
            viewModel.StatusMessage.Contains("Settings_", StringComparison.Ordinal),
            "The person is shown a sentence, never a raw resource key.");
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The row write is raised from a property-changed handler, so it completes on a later turn of the loop.
    /// Polls rather than sleeping blindly, and fails rather than returning a false green.
    /// </summary>
    private static async Task WaitForWriteAsync(FakeRequestItemAllottedMinutesStore store, int expectedWrites = 1)
    {
        for (var attempt = 0; attempt < 100 && store.Writes.Count < expectedWrites; attempt++)
        {
            await Task.Delay(20);
        }

        Assert.IsTrue(
            store.Writes.Count >= expectedWrites,
            $"Expected at least {expectedWrites} write(s) to the Item's stored allotment, saw {store.Writes.Count}.");
    }
}
