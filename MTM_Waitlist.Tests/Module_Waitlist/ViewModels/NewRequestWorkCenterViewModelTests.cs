using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class NewRequestWorkCenterViewModelTests
{
    [TestMethod]
    public void OnNavigatedTo_LoadsCatalogPopulatesHotAndOtherWithDetail()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: new[] { "Press 1" },
                other: new[] { "Press 2" },
                active: new[] { "Press 1", "Press 2" },
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new()
                    {
                        Building = "Expo Drive",
                        LastUpdatedUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
                        HasActiveJob = true,
                        CurrentWorkOrder = "WO-100",
                        CurrentPartNumber = "PART-1",
                        CurrentSequenceNumber = "10",
                    },
                    ["Press 2"] = new()
                    {
                        Building = "Expo Drive",
                        HasActiveJob = true,
                        CurrentWorkOrder = "WO-200",
                        CurrentPartNumber = "PART-2",
                        CurrentSequenceNumber = "20",
                    },
                }),
        };
        var navigationService = new RecordingNavigationService();
        var viewModel = CreateViewModel(catalogService, new StubBuildingSelectionService(), navigationService);

        viewModel.OnNavigatedTo(new NewRequestFlowState());

        Assert.AreEqual(1, viewModel.HotWorkCenters.Count);
        Assert.AreEqual(1, viewModel.OtherWorkCenters.Count);

        var hot = viewModel.HotWorkCenters[0];
        Assert.AreEqual("Press 1", hot.WorkCenterName);
        Assert.AreEqual("Expo Drive", hot.Building);
        Assert.IsTrue(hot.HasActiveJob);
        Assert.AreEqual("WO-100", hot.CurrentWorkOrder);
        Assert.AreEqual("PART-1", hot.CurrentPartNumber);
        Assert.AreEqual("10", hot.CurrentSequenceNumber);
        Assert.IsFalse(hot.IsSelected);

        var other = viewModel.OtherWorkCenters[0];
        Assert.AreEqual("Press 2", other.WorkCenterName);
        Assert.AreEqual("WO-200", other.CurrentWorkOrder);
        Assert.AreEqual("PART-2", other.CurrentPartNumber);
        Assert.AreEqual("20", other.CurrentSequenceNumber);
    }

    [TestMethod]
    public void ApplyFilter_FiltersBySelectedBuildingAndOnBuildingChange()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: new[] { "Press 1", "Press 2" },
                other: Array.Empty<string>(),
                active: new[] { "Press 1", "Press 2" },
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new() { Building = "Expo Drive" },
                    ["Press 2"] = new() { Building = "Vits Drive" },
                }),
        };
        var buildingService = new StubBuildingSelectionService(selectedBuilding: "Vits Drive");
        var viewModel = CreateViewModel(catalogService, buildingService, new RecordingNavigationService());

        viewModel.OnNavigatedTo(new NewRequestFlowState());

        Assert.AreEqual(1, viewModel.HotWorkCenters.Count);
        Assert.AreEqual("Press 2", viewModel.HotWorkCenters[0].WorkCenterName);

        buildingService.SelectedBuilding = "Expo Drive";

        Assert.AreEqual(1, viewModel.HotWorkCenters.Count);
        Assert.AreEqual("Press 1", viewModel.HotWorkCenters[0].WorkCenterName);
    }

    [TestMethod]
    public void ApplyFilter_FiltersByFilterTextAcrossNameAndJobDetail()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: new[] { "Press 1", "Press 2", "Press 3" },
                other: Array.Empty<string>(),
                active: new[] { "Press 1", "Press 2", "Press 3" },
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new() { Building = "Expo Drive", CurrentWorkOrder = "WO-100", CurrentPartNumber = "PART-1" },
                    ["Press 2"] = new() { Building = "Expo Drive", CurrentWorkOrder = "WO-200", CurrentPartNumber = "PART-2" },
                    ["Press 3"] = new() { Building = "Expo Drive", CurrentWorkOrder = "WO-300", CurrentPartNumber = "PART-3" },
                }),
        };
        var viewModel = CreateViewModel(catalogService, new StubBuildingSelectionService(), new RecordingNavigationService());

        viewModel.OnNavigatedTo(new NewRequestFlowState());
        Assert.AreEqual(3, viewModel.HotWorkCenters.Count);

        viewModel.FilterText = "WO-200";

        Assert.AreEqual(1, viewModel.HotWorkCenters.Count);
        Assert.AreEqual("Press 2", viewModel.HotWorkCenters[0].WorkCenterName);

        viewModel.FilterText = "press 3";

        Assert.AreEqual(1, viewModel.HotWorkCenters.Count);
        Assert.AreEqual("Press 3", viewModel.HotWorkCenters[0].WorkCenterName);

        viewModel.FilterText = string.Empty;

        Assert.AreEqual(3, viewModel.HotWorkCenters.Count);
    }

    [TestMethod]
    public void LoadWorkCenters_NoLocalHidesLocalSectionAndExpandsOther()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: Array.Empty<string>(),
                other: new[] { "Press 1", "Press 2" },
                active: new[] { "Press 1", "Press 2" },
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new() { Building = "Expo Drive" },
                    ["Press 2"] = new() { Building = "Expo Drive" },
                }),
        };
        var viewModel = CreateViewModel(catalogService, new StubBuildingSelectionService(), new RecordingNavigationService());

        viewModel.OnNavigatedTo(new NewRequestFlowState());

        Assert.IsFalse(viewModel.IsLocalWorkCentersVisible);
        Assert.IsTrue(viewModel.IsOtherWorkCentersExpanded);
        Assert.AreEqual(2, viewModel.OtherWorkCenters.Count);
        Assert.AreEqual(0, viewModel.HotWorkCenters.Count);
    }

    [TestMethod]
    public void OtherWorkCentersHeader_TracksExpandedState()
    {
        var viewModel = CreateViewModel(
            new FakeWorkCenterCatalogService(),
            new StubBuildingSelectionService(),
            new RecordingNavigationService());

        Assert.IsFalse(viewModel.IsOtherWorkCentersExpanded);
        Assert.AreEqual("Show Other Work Centers", viewModel.OtherWorkCentersHeader);

        viewModel.IsOtherWorkCentersExpanded = true;

        Assert.AreEqual("Hide Other Work Centers", viewModel.OtherWorkCentersHeader);
    }

    [TestMethod]
    public void SelectWorkCenter_MarksSelectedClearsOthersAndNavigates()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: new[] { "Press 1", "Press 2" },
                other: Array.Empty<string>(),
                active: new[] { "Press 1", "Press 2" },
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new() { Building = "Expo Drive", HasActiveJob = true },
                    ["Press 2"] = new() { Building = "Expo Drive", HasActiveJob = true },
                }),
        };
        var navigationService = new RecordingNavigationService();
        var state = new NewRequestFlowState();
        var viewModel = CreateViewModel(catalogService, new StubBuildingSelectionService(), navigationService);

        viewModel.OnNavigatedTo(state);
        var first = viewModel.HotWorkCenters[0];
        var second = viewModel.HotWorkCenters[1];

        viewModel.SelectWorkCenterCommand.Execute(second);

        Assert.IsFalse(first.IsSelected);
        Assert.IsTrue(second.IsSelected);
        Assert.AreEqual("Press 2", state.WorkCenter);
        Assert.AreEqual("6229", state.RequesterEmployeeNumber);
        Assert.IsFalse(string.IsNullOrWhiteSpace(state.RequesterEmployeeName));
        Assert.AreEqual(1, navigationService.Navigations.Count);
        Assert.AreEqual(typeof(NewRequestJobTypeViewModel).FullName, navigationService.Navigations[0].PageKey);
        Assert.AreSame(state, navigationService.Navigations[0].Parameter);
    }

    [TestMethod]
    public void SelectWorkCenter_BlocksWorkCenterWithoutActiveJob()
    {
        var catalogService = new FakeWorkCenterCatalogService
        {
            Catalog = BuildCatalog(
                hot: new[] { "Press 1" },
                other: Array.Empty<string>(),
                active: Array.Empty<string>(),
                details: new Dictionary<string, WorkCenterDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Press 1"] = new() { Building = "Expo Drive", HasActiveJob = false },
                }),
        };
        var navigationService = new RecordingNavigationService();
        var viewModel = CreateViewModel(catalogService, new StubBuildingSelectionService(), navigationService);

        viewModel.OnNavigatedTo(new NewRequestFlowState());
        var item = viewModel.HotWorkCenters[0];

        viewModel.SelectWorkCenterCommand.Execute(item);

        Assert.IsTrue(viewModel.IsNoActiveJobWarningVisible);
        Assert.IsFalse(viewModel.IsVerificationWarningVisible);
        Assert.AreEqual(0, navigationService.Navigations.Count);
    }

    [TestMethod]
    public void WorkCenterSelectionItem_ComputedDisplays_FormatJobPartAndLastUpdated()
    {
        var full = new WorkCenterSelectionItem
        {
            WorkCenterName = "Press 1",
            CurrentWorkOrder = "WO-100",
            CurrentSequenceNumber = "10",
            CurrentPartNumber = "PART-1",
            LastUpdatedUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        Assert.AreEqual("WO-100/10", full.CurrentJobSummary);
        Assert.AreEqual("PART-1", full.CurrentPartSummary);
        Assert.AreEqual(
            full.LastUpdatedUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
            full.LastUpdatedDisplay);

        var workOrderOnly = new WorkCenterSelectionItem { CurrentWorkOrder = "WO-100" };
        Assert.AreEqual("WO-100", workOrderOnly.CurrentJobSummary);

        var empty = new WorkCenterSelectionItem();
        Assert.AreEqual("None", empty.CurrentJobSummary);
        Assert.AreEqual("None", empty.CurrentPartSummary);
        Assert.AreEqual("Never", empty.LastUpdatedDisplay);
    }

    [TestMethod]
    public void WorkCenterSelectionItem_LastUpdatedDisplay_FormatsLocalTimeAndNeverWhenNull()
    {
        var withDate = new WorkCenterSelectionItem
        {
            LastUpdatedUtc = new DateTime(2026, 8, 1, 18, 30, 0, DateTimeKind.Utc),
        };
        var expected = withDate.LastUpdatedUtc!.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
        Assert.AreEqual(expected, withDate.LastUpdatedDisplay);

        var withoutDate = new WorkCenterSelectionItem();
        Assert.AreEqual("Never", withoutDate.LastUpdatedDisplay);
    }

    private static NewRequestWorkCenterViewModel CreateViewModel(
        FakeWorkCenterCatalogService catalogService,
        StubBuildingSelectionService buildingService,
        RecordingNavigationService navigationService)
    {
        return new NewRequestWorkCenterViewModel(
            navigationService,
            catalogService,
            new FakeNewRequestFlowService(),
            buildingService);
    }

    private static WorkCenterCatalogResult BuildCatalog(
        IReadOnlyList<string> hot,
        IReadOnlyList<string> other,
        IReadOnlyList<string> active,
        IReadOnlyDictionary<string, WorkCenterDetail> details)
    {
        return new WorkCenterCatalogResult
        {
            ComputerName = "test-workstation",
            HotWorkCenters = hot,
            OtherWorkCenters = other,
            ActiveJobWorkCenters = active,
            WorkCenterDetails = details,
        };
    }

    private sealed class FakeWorkCenterCatalogService : IWorkCenterCatalogService
    {
        public WorkCenterCatalogResult Catalog { get; set; } = new();

        public string GetCurrentComputerName() => "test-workstation";

        public Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ComputerOption>>(new[] { new ComputerOption { Key = "test-workstation", Label = "Test Workstation - test-workstation" } });

        public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Catalog);

        public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class FakeNewRequestFlowService : INewRequestFlowService
    {
        public Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewRequestTypeDefinition>>(Array.Empty<NewRequestTypeDefinition>());

        public Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> ResolveRequestSubtypeImagePathAsync(string requestTypeName, string subtypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, string>());
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        private string _selectedBuilding;

        public event EventHandler? BuildingChanged;

        public IReadOnlyList<string> Buildings { get; } = new[] { "Expo Drive", "Vits Drive" };

        public string SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || string.Equals(_selectedBuilding, value, StringComparison.Ordinal))
                {
                    return;
                }

                _selectedBuilding = value;
                BuildingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public StubBuildingSelectionService(string selectedBuilding = "Expo Drive")
        {
            _selectedBuilding = selectedBuilding;
        }
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Frame? Frame { get; set; }

        public int GoBackCallCount { get; private set; }

        public List<(string PageKey, object? Parameter)> Navigations { get; } = new();

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            Navigations.Add((pageKey, parameter));
            return true;
        }

        public bool GoBack()
        {
            GoBackCallCount++;
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
