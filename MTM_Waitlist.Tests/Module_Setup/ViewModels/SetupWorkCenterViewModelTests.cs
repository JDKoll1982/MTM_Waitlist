using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Tests.Module_Setup.ViewModels;

/// <summary>
/// The setup work-center search box behaves the way the New Request step's does: a search looks past this
/// computer's own work centers, so the "Other Work Centers" list opens while the box has text and closes again
/// when it is cleared.
/// </summary>
[TestClass]
public sealed class SetupWorkCenterViewModelTests
{
    [TestMethod]
    public async Task ViewModel_SetupWorkCenterViewModelTests_FilterText_OpensTheOtherWorkCentersWhileItHasText()
    {
        var viewModel = CreateViewModel(hot: new[] { "Press 1" }, other: new[] { "Press 2" });

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.IsFalse(viewModel.IsOtherWorkCentersExpanded, "an empty box leaves the other work centers closed");

        viewModel.FilterText = "press";

        Assert.IsTrue(viewModel.IsOtherWorkCentersExpanded, "a search opens them, so the matches are all on screen");

        viewModel.FilterText = "   ";

        Assert.IsFalse(viewModel.IsOtherWorkCentersExpanded, "whitespace is not a search");
    }

    [TestMethod]
    public async Task ViewModel_SetupWorkCenterViewModelTests_FilterText_ComputerWithNoLocalWorkCentersKeepsTheOthersOpen()
    {
        var viewModel = CreateViewModel(hot: Array.Empty<string>(), other: new[] { "Press 2" });

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsOtherWorkCentersExpanded, "with nothing of its own, this computer shows the others");

        viewModel.FilterText = "press";

        Assert.IsTrue(viewModel.IsOtherWorkCentersExpanded);

        viewModel.FilterText = string.Empty;

        Assert.IsTrue(viewModel.IsOtherWorkCentersExpanded, "collapsing here would leave the page with no cards at all");
    }

    private static SetupWorkCenterViewModel CreateViewModel(IReadOnlyList<string> hot, IReadOnlyList<string> other)
    {
        var catalogService = new StubWorkCenterCatalogService
        {
            Catalog = new WorkCenterCatalogResult
            {
                ComputerName = "PRESS-3",
                HotWorkCenters = hot,
                OtherWorkCenters = other,
            },
        };

        var workCenterService = new StubSetupWorkCenterService
        {
            WorkCenters = hot.Concat(other)
                .Select(name => new SetupWorkCenter { Id = string.Empty, Name = name })
                .ToList(),
        };

        return new SetupWorkCenterViewModel(
            new NoOpNavigationService(),
            new StubSetupWorkflowService(),
            workCenterService,
            new StubWorkCenterImageService(),
            catalogService,
            new StubBuildingSelectionService(),
            new StubPermissionService());
    }

    private sealed class StubSetupWorkCenterService : ISetupWorkCenterService
    {
        public IReadOnlyList<SetupWorkCenter> WorkCenters { get; init; } = Array.Empty<SetupWorkCenter>();

        public Task<IReadOnlyList<SetupWorkCenter>> GetWorkCentersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkCenters);

        public Task<SetupSelectionResult> AddWorkCenterAsync(string workstationName, string building, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> UpdateWorkCenterAsync(string workstationId, string workstationName, string building, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveWorkCenterAsync(string workstationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SetWorkCenterGenericAsync(string workstationId, bool isGeneric, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<IReadOnlyList<SetupWorkCenterHistoryEntry>> GetWorkCenterHistoryAsync(string workCenterName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupWorkCenterHistoryEntry>>(Array.Empty<SetupWorkCenterHistoryEntry>());
    }

    private sealed class StubSetupWorkflowService : ISetupWorkflowService
    {
        public SetupWorkflowState State { get; } = new();

        public bool HasUnsavedChanges => State.HasUnsavedChanges;

        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SetupLookupResult> SearchWorkOrderAsync(string workOrderInput, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupLookupResult { Success = true });

        public Task<SetupSelectionResult> SelectPartAsync(string partNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectSequenceAsync(string sequenceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnageTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> SelectDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> AddDunnagePartToPairAsync(SetupDunnagePart part, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveDunnagePartAsync(string dunnagePartId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> RemoveAllDunnageForTypeAsync(string dunnageTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSelectionResult> ClearAllDunnageForPairAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult { Success = true });

        public Task<SetupSaveResult> SaveAsync(bool forceReplace = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSaveResult { Success = true });
    }

    private sealed class StubWorkCenterImageService : IWorkCenterImageService
    {
        public bool IsInitialized => true;

        public Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<string> ResolveWorkCenterImagePathAsync(string workCenterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);
    }

    private sealed class StubWorkCenterCatalogService : IWorkCenterCatalogService
    {
        public WorkCenterCatalogResult Catalog { get; init; } = new();

        public string GetCurrentComputerName() => Catalog.ComputerName;

        public Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ComputerOption>>(Array.Empty<ComputerOption>());

        public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Catalog);

        public Task<IReadOnlyList<string>> GetHotWorkCenterNamesAsync(string workstationName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Catalog.HotWorkCenters);

        public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? BuildingsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<string> Buildings => Array.Empty<string>();

        public string SelectedBuilding { get; set; } = BuildingScope.AllBuildings;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubPermissionService : IPermissionService
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
