using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Startup.Models;

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

    [TestMethod]
    public async Task SelectDunnageTypeAndPart_MovesToReviewAndCarriesSummary()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");

        var typeResult = await service.SelectDunnageTypeAsync("Coils");
        Assert.IsTrue(typeResult.Success);
        Assert.AreEqual(SetupWorkflowStep.DunnagePartSelection, service.State.CurrentStep);
        Assert.AreEqual("Coils", service.State.SelectedDunnageTypeId);
        Assert.IsTrue(service.State.DunnageParts.Count > 0);

        var partResult = await service.SelectDunnagePartAsync("coil-a");
        Assert.IsTrue(partResult.Success);
        Assert.AreEqual(SetupWorkflowStep.DunnageTypeSelection, service.State.CurrentStep);
        Assert.AreEqual("coil-a", service.State.SelectedDunnagePartId);
        Assert.IsTrue(service.State.SelectedDunnageSummary.Contains("Dunnage Coil A", StringComparison.OrdinalIgnoreCase));
    }

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
    public async Task SelectDunnagePartAsync_AllowsMultipleAssignmentsForPair()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");
        await service.SelectDunnageTypeAsync("Coils");

        var addFirst = await service.SelectDunnagePartAsync("coil-a");
        var addSecond = await service.SelectDunnagePartAsync("coil-b");

        Assert.IsTrue(addFirst.Success);
        Assert.IsTrue(addSecond.Success);
        Assert.AreEqual(2, service.State.SelectedDunnageParts.Count);
        Assert.IsTrue(service.State.SelectedDunnageParts.Any(part => part.Id == "coil-a"));
        Assert.IsTrue(service.State.SelectedDunnageParts.Any(part => part.Id == "coil-b"));
    }

    [TestMethod]
    public async Task RemoveDunnagePartAsync_RemovesSingleAssignedItem()
    {
        var service = CreateService();

        await service.SearchWorkOrderAsync("76951");
        await service.SelectPartAsync("12345679");
        await service.SelectSequenceAsync("20");
        await service.SelectDunnageTypeAsync("Coils");
        await service.SelectDunnagePartAsync("coil-a");
        await service.SelectDunnagePartAsync("coil-b");

        var removeResult = await service.RemoveDunnagePartAsync("coil-a");

        Assert.IsTrue(removeResult.Success);
        Assert.AreEqual(1, service.State.SelectedDunnageParts.Count);
        Assert.IsFalse(service.State.SelectedDunnageParts.Any(part => part.Id == "coil-a"));
        Assert.IsTrue(service.State.SelectedDunnageParts.Any(part => part.Id == "coil-b"));
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
        var sampleDataService = new SampleDataService(settings);
        return new DunnageWorkflowService(new MySqlHelperServer(settings, sampleDataService));
    }

    private static SetupWorkflowService CreateService(bool recvMockData = true)
    {
        var state = new SetupWorkflowState();
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = true,
            ["Feature.RecvMockData"] = recvMockData,
        });
        var sampleDataService = new SampleDataService(settings);
        var sqlHelperServer = new SqlHelperServer(settings, sampleDataService);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        var workOrderValidationService = new WorkOrderValidationService();
        var lookupService = new SetupLookupService(sqlHelperServer, new InforVisualSqlQueryService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()));
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