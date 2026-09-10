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

        var result = await service.SearchWorkOrderAsync("76951");

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

        await service.SearchWorkOrderAsync("76951");
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

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");

        var sequenceResult = await service.SelectSequenceAsync("20");

        Assert.IsTrue(sequenceResult.Success);
        Assert.AreEqual(SetupWorkflowStep.DunnageTypeSelection, service.State.CurrentStep);
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Coil", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Die", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Category, "Component", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SelectSequenceAsync_OmitsSubordinatePartsAtDefaultIgnoredPlantLocations()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        var ignored = new[] { "WC", "NCM", "V-WC", "NCM-VITS", "SHIP" };
        Assert.IsFalse(service.State.SubordinateParts.Any(part =>
            !string.IsNullOrWhiteSpace(part.Location) &&
            ignored.Contains(part.Location.Trim().ToUpperInvariant())));
        // Non-ignored rack rows still load after the ignored plant-code rows (NCM/SHIP) are removed.
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Location, "Rack A1", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Location, "Kit Shelf 2", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SelectSequenceAsync_OmitsSubordinatePartsAtCustomIgnoredLocations()
    {
        var service = CreateService(ignoredLocations: new[] { "Kit Shelf 2" });

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        Assert.IsFalse(service.State.SubordinateParts.Any(part => string.Equals(part.Location, "Kit Shelf 2", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(service.State.SubordinateParts.Any(part => string.Equals(part.Location, "Rack A1", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SelectSequenceAsync_WhenRecvMockDisabled_DoesNotLoadMockDunnageTypes()
    {
        var service = CreateService(recvMockData: false);

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");

        var sequenceResult = await service.SelectSequenceAsync("20");

        Assert.IsTrue(sequenceResult.Success);
        Assert.AreEqual(SetupWorkflowStep.DunnageTypeSelection, service.State.CurrentStep);
        Assert.AreEqual(0, service.State.DunnageTypes.Count);
    }

    [TestMethod]
    public async Task ClearAllDunnageForPairAsync_RemovesAllAssignedItems()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("76951");
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
        var result = await CreateDunnageWorkflowService().AddDunnageTypeAsync(string.Empty, "Developer");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AddDunnagePartAsync_WhenTypeIdIsInvalid_ReturnsValidationFailure()
    {
        var result = await CreateDunnageWorkflowService().AddDunnagePartAsync("not-an-id", "Test Part", "Developer");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("valid dunnage type", StringComparison.OrdinalIgnoreCase));
    }

    private static DunnageWorkflowService CreateDunnageWorkflowService()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
            ["Feature.RecvMockData"] = false,
        });
        return new DunnageWorkflowService(new MySqlHelperServer());
    }

    private static SetupWorkflowService CreateService(bool recvMockData = true, IReadOnlyList<string>? ignoredLocations = null)
    {
        var state = new SetupWorkflowState();
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
            ["Feature.RecvMockData"] = recvMockData,
        });
        if (ignoredLocations is { Count: > 0 })
        {
            settings.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, ignoredLocations.ToList()).GetAwaiter().GetResult();
        }

        var mySqlHelperServer = new MySqlHelperServer();
        var workOrderValidationService = new WorkOrderValidationService();

        // The Setup lookups are now served by the read-shape fallbacks, so the workflow tests inject
        // fakes backed by the same catalogue content the retired mock system used to supply.
        // TODO(T042/T043): retire these fakes together with SetupDataCatalog.
        var lookupService = new SetupLookupService(
            new FakeVisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow>(
                request => SetupDataCatalog.GetParts(request.NormalizedWorkOrder)
                    .Select(part => new VisualWorkOrderLookupRow
                    {
                        PartNumber = part.PartNumber,
                        Description = part.Description,
                        WorkCenter = part.WorkCenter,
                    })
                    .ToArray()),
            new FakeVisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow>(
                request => SetupDataCatalog.GetSequences(request.NormalizedWorkOrder, request.PartNumber)
                    .Select(sequence => new VisualOperationSequenceRow
                    {
                        SequenceNumber = sequence.SequenceNumber,
                        Description = sequence.Description,
                    })
                    .ToArray()),
            new FakeVisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow>(
                request => SetupDataCatalog.GetSubordinateParts(request.NormalizedWorkOrder, request.PartNumber, request.SequenceNumber)
                    .Select(part => new VisualSubordinatePartRow
                    {
                        Category = part.Category,
                        PartNumber = part.PartNumber,
                        Description = part.Description,
                        Location = part.Location,
                        User8 = part.User8,
                        OnHandQuantity = part.OnHandQuantity,
                    })
                    .ToArray()),
            new IgnoredLocationsService(settings));
        var dunnageWorkflowService = new DunnageWorkflowService(mySqlHelperServer);
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