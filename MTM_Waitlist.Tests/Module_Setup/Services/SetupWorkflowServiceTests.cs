using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupWorkflowServiceTests
{
    [TestMethod]
    public async Task SearchWorkOrderAsync_MultiplePartsMovesToPartSelection()
    {
        var service = CreateService();

        var result = await service.SearchWorkOrderAsync("WO-076951");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SetupWorkflowStep.PartSelection, service.State.CurrentStep);
        Assert.AreEqual("WO-076951", service.State.NormalizedWorkOrder);
        Assert.AreEqual(3, service.State.PartResults.Count);
    }

    [TestMethod]
    public async Task SearchWorkOrderAsync_SinglePartMovesToSequenceSelection()
    {
        var service = CreateService();

        var result = await service.SearchWorkOrderAsync("WO-076952");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SetupWorkflowStep.SequenceSelection, service.State.CurrentStep);
        Assert.AreEqual(1, service.State.PartResults.Count);
        Assert.AreEqual(1, service.State.SequenceResults.Count);
    }

    [TestMethod]
    public async Task SaveAsync_WhenActiveJobExistsRequestsReplacementConfirmation()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("WO-076951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        var result = await service.SaveAsync(false);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.RequiresReplacementConfirmation);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    // The dunnage type/part selection tests that previously ran against the retired sample catalog
    // (SetupDataCatalog) were removed together with the mock seam (FR-001/FR-014). Selecting a dunnage
    // type or part now reads the live receiving store, so that coverage moves to a live-database
    // integration test rather than being asserted against sample rows.

    [TestMethod]
    public async Task SelectSequenceAsync_LoadsSubordinatePartsForReviewContext()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("WO-076951");
        await service.SelectPartAsync("12345679");

        var sequenceResult = await service.SelectSequenceAsync("20");

        Assert.IsTrue(sequenceResult.Success);
        Assert.AreEqual(SetupWorkflowStep.DunnageTypeSelection, service.State.CurrentStep);
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Coil", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Die", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Component", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SelectSequenceAsync_KeepsSubordinatePartsWhoseLocationIsAPlantCode()
    {
        // Setup does not consult the ignored-locations set: that set keeps plant inventory codes out of the
        // waitlist's inventory location LISTS, and applying it here removed whole parts from the job.
        var service = CreateService();

        await service.SearchWorkOrderAsync("WO-076951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        foreach (var plantCode in new[] { "WC", "NCM", "SHIP" })
        {
            Assert.IsTrue(
                service.State.SubordinateParts.Any(part =>
                    string.Equals(part.Location, plantCode, StringComparison.OrdinalIgnoreCase)),
                $"The subordinate part at '{plantCode}' is missing, so Setup is still filtering on ignored locations.");
        }
    }

    [TestMethod]
    public async Task SelectSequenceAsync_KeepsTheJobsCoilAtItsWorkCentreLocation()
    {
        // The job's own coil, as WO-074171 has it: issued to the work centre, so Infor Visual reports its location
        // as 'WC'. Dropping it made the review, the saved record and the waitlist's coil availability all see a
        // job with no coil while Infor Visual carried one all along.
        var service = CreateService();

        await service.SearchWorkOrderAsync("WO-076951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        var coil = service.State.SubordinateParts
            .SingleOrDefault(part => string.Equals(part.PartNumber, "MMC0000887", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(coil, "The job's coil is missing from the subordinate parts.");
        Assert.AreEqual("Coil", coil!.Category);
        Assert.AreEqual("WC", coil.Location, "The coil is at the work centre; its location is reported as it comes.");
    }

    [TestMethod]
    public async Task ClearAllDunnageForPairAsync_RemovesAllAssignedItems()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("WO-076951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");
        await service.SelectDunnageTypeAsync("Coils");
        await service.SelectDunnagePartAsync("coil-a");
        await service.SelectDunnagePartAsync("coil-b");

        var clearResult = await service.ClearAllDunnageForPairAsync();

        Assert.IsTrue(clearResult.Success);
        Assert.AreEqual(0, service.State.SelectedDunnageParts.Count);
    }

    [TestMethod]
    public async Task AddDunnageTypeAsync_WhenNameIsMissing_ReturnsValidationFailure()
    {
        var result = await CreateDunnageWorkflowService().AddDunnageTypeAsync(string.Empty);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AddDunnagePartAsync_WhenTypeIdIsInvalid_ReturnsValidationFailure()
    {
        var result = await CreateDunnageWorkflowService().AddDunnagePartAsync("not-an-id", "Test Part");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("valid dunnage type", StringComparison.OrdinalIgnoreCase));
    }

    private static DunnageWorkflowService CreateDunnageWorkflowService() =>
        new(new MySqlHelperServer(), permissionService: new AlwaysPermittingPermissionService());

    private static SetupWorkflowService CreateService()
    {
        var state = new SetupWorkflowState();

        // No mock toggle is seeded: internal stores are always live and the retired demo toggles no longer exist
        // (FR-003/FR-014). The Setup reads take no settings at all — the ignored-locations set is the waitlist's
        // inventory rule, and Setup no longer consults it.
        var mySqlHelperServer = new MySqlHelperServer();
        var workOrderValidationService = new WorkOrderValidationService();

        // The Setup lookups are served by the read-shape fallbacks, so the workflow tests inject fakes
        // backed by fixture data this test owns. The production sample catalog they used to share was
        // retired with the legacy mock system (FR-014, SC-013).
        var lookupService = new SetupLookupService(
            new FakeVisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow>(
                request => SetupLookupFixtureData.GetParts(request.NormalizedWorkOrder)),
            new FakeVisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow>(
                request => SetupLookupFixtureData.GetSequences(request.NormalizedWorkOrder, request.PartNumber)),
            new FakeVisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow>(
                request => SetupLookupFixtureData.GetSubordinateParts(request.NormalizedWorkOrder, request.PartNumber, request.SequenceNumber)));
        var dunnageWorkflowService = new DunnageWorkflowService(mySqlHelperServer, permissionService: new AlwaysPermittingPermissionService());
        var activeJobCoordinatorService = new SetupActiveJobCoordinatorService();
        var persistenceService = new SetupPersistenceService(activeJobCoordinatorService, mySqlHelperServer);

        return new SetupWorkflowService(
            workOrderValidationService,
            lookupService,
            lookupService,
            dunnageWorkflowService,
            persistenceService,
            state);
    }

    /// <summary>
    /// A permission service that admits everything, so a test about something else reaches the code beneath the
    /// gate. The gate itself is proved where it lives, in <c>SetupDunnageWorkflowServiceTests</c>.
    /// </summary>
    private sealed class AlwaysPermittingPermissionService : IPermissionService
    {
        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => true, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
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

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public Microsoft.UI.Xaml.Controls.Frame? Frame
        {
            get => null;
            set { }
        }

        public bool CanGoBack => false;

        public bool GoBack() => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}