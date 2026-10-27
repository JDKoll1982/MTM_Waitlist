using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class WaitlistViewDetailViewModelTests
{
    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_WhenPassedASubmittedRequestIdLoadsMatchingItemAndTemplateSectionsAsync()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitRequestAsync(requestService);
        var item = WaitlistViewViewModel.CreateSessionOrder(request);

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(item.Id);

        Assert.IsNotNull(viewModel.Item);
        Assert.AreEqual(item.Id, viewModel.Item!.Id);
        // One card, not two: the second said only what the hero card above it already says (2026-10-16).
        Assert.AreEqual(1, viewModel.TemplateSections.Count);
        Assert.AreEqual("Request", viewModel.TemplateSections[0].Title);
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_WhenPassedSessionRequestIdUsesTheCorrectRequestDetailTemplateAsync()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitRequestAsync(requestService, itemCode: "deliver-wrong-coil", inputValue: "Wrong material at press");
        var item = WaitlistViewViewModel.CreateSessionOrder(request);

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(item.Id);

        // One block, not two: the Item the request names, and everything the request itself carries. The Item row
        // is the Item's umbrella phrase, so the wrong-material Item reads its own pinned first line rather than
        // the plain Category word (contract §2, FR-005). The second block that used to follow it stated the work
        // centre, the person who asked and the remaining time — all three already on the hero card above — and is
        // gone rather than emptied (2026-10-16).
        Assert.AreEqual(1, viewModel.TemplateSections.Count);
        Assert.AreEqual("Request", viewModel.TemplateSections[0].Title);
        Assert.AreEqual("Wrong Coil Bring:", SectionField(viewModel.TemplateSections[0], "Item"));
        Assert.AreEqual("Wrong material at press", SectionField(viewModel.TemplateSections[0], "Request details"));
    }

    private static async Task<WaitlistRequest> SubmitRequestAsync(
        WaitlistRequestService requestService,
        string itemCode = "pickup-coil",
        string inputValue = "COIL-204")
    {
        var definition = RequestItemCatalog.FindById(itemCode);
        Assert.IsNotNull(definition, $"The fixture names '{itemCode}', which is not in the Item catalog.");

        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "100-03",
            Category = definition!.Category.ToString(),
            Item = itemCode,
            InputValue = inputValue,
            ActiveSetupJobId = "100-03",
            WorkCenterName = "100-03",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var submit = await requestService.SubmitAsync(draft);
        Assert.IsNotNull(submit.Request);
        return submit.Request!;
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailViewModelTests_BackCommand_CallsNavigationGoBack()
    {
        var navigationService = new RecordingNavigationService();
        var viewModel = new WaitlistViewDetailViewModel(
            navigationService,
            new StubBuildingSelectionService());

        viewModel.BackCommand.Execute(null);

        Assert.AreEqual(1, navigationService.GoBackCallCount);
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_RealSubmittedCoilRequestResolvesNonNullItemAsync()
    {
        var requestService = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "100-03",
            Category = RequestCategory.Pickup.ToString(),
            Item = "pickup-coil",
            ActiveSetupJobId = "100-03",
            WorkCenterName = "100-03",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };
        var submit = await requestService.SubmitAsync(draft);
        var requestId = submit.Request!.Id;

        // No sample rows exist any more: the item must be resolved from the request service.
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(requestId.GetHashCode());

        Assert.IsNotNull(viewModel.Item);
        Assert.AreEqual(requestId.GetHashCode(), viewModel.Item!.Id);
        Assert.AreEqual("pickup-coil", viewModel.Item!.ItemCode, "The page must carry the Item the request was raised with.");
        Assert.IsTrue(
            viewModel.TemplateSections.Any(section => string.Equals(section.Title, "Request", StringComparison.OrdinalIgnoreCase)),
            "A resolved request must render its block.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_CoilRequestWorkOrderAndRequestShowsRealRequestValuesAsync()
    {
        var requestService = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "100-03",
            Category = RequestCategory.Pickup.ToString(),
            Item = "pickup-coil",
            // The stored job id is the work centre here on purpose: that is what every request raised before
            // 2026-10-16 carries, and the Work order row has to read a work order for those rows too.
            ActiveSetupJobId = "100-03",
            WorkCenterName = "100-03",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };
        var submit = await requestService.SubmitAsync(draft);
        var requestId = submit.Request!.Id;

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService,
            jobAvailabilityProvider: new StubJobAvailabilityProvider("WO-046885"));

        viewModel.OnNavigatedTo(requestId.GetHashCode());

        var requestSection = viewModel.TemplateSections.First(section =>
            string.Equals(section.Title, "Request", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(
            "WO-046885",
            SectionField(requestSection, "Work order"),
            "The Work order row states the job's own work order, never the work centre the request stored.");
        Assert.IsNull(
            SectionField(requestSection, "Work center"),
            "The hero card above already states the work centre, so this card does not say it again.");
        Assert.IsNull(
            SectionField(requestSection, "Remaining time"),
            "Nor the remaining time, which the hero card states against its allotment.");
    }

    private static string? SectionField(WaitlistDetailTemplateSection section, string label)
        => section.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>
    /// The requesting work centre's job as this page reads it. It answers with the work order the job is for,
    /// which is where the page's Work order row gets its value from: the request itself never stored a real one.
    /// It answers synchronously, on purpose, so the page finishes its job read before the assertions run.
    /// </summary>
    private sealed class StubJobAvailabilityProvider(string workOrder) : IRequestJobPartAvailabilityProvider
    {
        public Task<RequestJobPartAvailability> GetAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(RequestJobPartAvailability.None with { HasActiveJob = true, WorkOrder = workOrder });
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_NoItemResolvedSetsFriendlyEmptyStateMessage()
    {
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService());   // no request service -> nothing resolves

        viewModel.OnNavigatedTo(9999);

        Assert.IsNull(viewModel.Item);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.EmptyStateMessage));
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailViewModelTests_OnNavigatedTo_ItemResolvedClearsEmptyStateMessageAsync()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitRequestAsync(requestService);
        var item = WaitlistViewViewModel.CreateSessionOrder(request);

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(item.Id);

        Assert.IsNotNull(viewModel.Item);
        Assert.AreEqual(string.Empty, viewModel.EmptyStateMessage);
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

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
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

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IReadOnlyList<string> Buildings => new[] { "Expo Drive" };

        public string SelectedBuilding { get; set; } = "Expo Drive";
    }
}