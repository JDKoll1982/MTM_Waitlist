using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class WaitlistViewDetailViewModelTests
{
    [TestMethod]
    public async Task OnNavigatedTo_WhenPassedASubmittedRequestId_LoadsMatchingItemAndTemplateSectionsAsync()
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
        Assert.IsTrue(viewModel.TemplateSections.Count >= 2);
    }

    [TestMethod]
    public async Task OnNavigatedTo_WhenPassedSessionRequestId_UsesTheCorrectRequestDetailTemplateAsync()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitRequestAsync(requestService, itemCode: "deliver-wrong-coil", inputValue: "Wrong material at press");
        var item = WaitlistViewViewModel.CreateSessionOrder(request);

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(item.Id);

        // Every request renders the same two blocks: the Item the request names, then the request's own
        // context. The first block's Item row is the Item's umbrella phrase, so the wrong-material Item reads
        // its own pinned first line rather than the plain Category word (contract §2, FR-005).
        Assert.AreEqual(2, viewModel.TemplateSections.Count);
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

        var submit = await requestService.SubmitAsync(draft, allowDuplicate: true);
        Assert.IsNotNull(submit.Request);
        return submit.Request!;
    }

    [TestMethod]
    public void BackCommand_CallsNavigationGoBack()
    {
        var navigationService = new RecordingNavigationService();
        var viewModel = new WaitlistViewDetailViewModel(
            navigationService,
            new StubBuildingSelectionService());

        viewModel.BackCommand.Execute(null);

        Assert.AreEqual(1, navigationService.GoBackCallCount);
    }

    [TestMethod]
    public async Task OnNavigatedTo_RealSubmittedCoilRequest_ResolvesNonNullItemAsync()
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
        var submit = await requestService.SubmitAsync(draft, allowDuplicate: false);
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
    public async Task OnNavigatedTo_CoilRequest_WorkOrderAndRequestShowsRealRequestValuesAsync()
    {
        var requestService = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "100-03",
            Category = RequestCategory.Pickup.ToString(),
            Item = "pickup-coil",
            ActiveSetupJobId = "WO-204",
            WorkCenterName = "100-03",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };
        var submit = await requestService.SubmitAsync(draft, allowDuplicate: false);
        var requestId = submit.Request!.Id;

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(requestId.GetHashCode());

        var requestSection = viewModel.TemplateSections.First(section =>
            string.Equals(section.Title, "Request", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("WO-204", SectionField(requestSection, "Work order"), "Work order should come from the request's active job id.");

        var contextSection = viewModel.TemplateSections.First(section =>
            string.Equals(section.Title, "Request context", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("100-03", SectionField(contextSection, "Work center"));
        Assert.AreEqual("John Koll", SectionField(contextSection, "Requesting user"));
        Assert.AreEqual("6229", SectionField(contextSection, "Employee number"), "Employee number should come from the requester.");
    }

    private static string? SectionField(WaitlistDetailTemplateSection section, string label)
        => section.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;

    [TestMethod]
    public void OnNavigatedTo_NoItemResolved_SetsFriendlyEmptyStateMessage()
    {
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService());   // no request service -> nothing resolves

        viewModel.OnNavigatedTo(9999);

        Assert.IsNull(viewModel.Item);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.EmptyStateMessage));
    }

    [TestMethod]
    public async Task OnNavigatedTo_ItemResolved_ClearsEmptyStateMessageAsync()
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

        public IReadOnlyList<string> Buildings => new[] { "Expo Drive" };

        public string SelectedBuilding { get; set; } = "Expo Drive";
    }
}