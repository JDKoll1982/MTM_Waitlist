using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class WaitlistViewDetailViewModelTests
{
    [TestMethod]
    public void OnNavigatedTo_WhenPassedIntId_LoadsMatchingItemAndTemplateSections()
    {
        var item = new SampleOrder
        {
            Id = 7,
            Title = "Coil Request",
            RequestedByName = "Jordan Lee",
            RequestedPressName = "Press 12",
            RemainingTimeText = "00:27"
        };

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(item),
            new StubBuildingSelectionService());

        viewModel.OnNavigatedTo(7);

        Assert.IsNotNull(viewModel.Item);
        Assert.AreEqual(7, viewModel.Item!.Id);
        Assert.AreEqual(3, viewModel.TemplateSections.Count);
    }

    [TestMethod]
    public void OnNavigatedTo_WhenPassedSessionRequestId_UsesTheCorrectRequestDetailTemplate()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Coil",
            Subtype = "Wrong Coil",
            InputValue = "Wrong material at press",
            Status = "Pending",
            RequestedUtc = DateTimeOffset.UtcNow,
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(12),
            IsOverdue = false,
        };

        var item = WaitlistViewViewModel.CreateSessionOrder(request);
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(item),
            new StubBuildingSelectionService());

        viewModel.OnNavigatedTo(item.Id);

        Assert.AreEqual(3, viewModel.TemplateSections.Count);
        Assert.AreEqual("Coil material", viewModel.TemplateSections[0].Title);
        Assert.AreEqual("Wrong coil", viewModel.TemplateSections[0].Fields[0].Value);
    }

    [TestMethod]
    public void BackCommand_CallsNavigationGoBack()
    {
        var navigationService = new RecordingNavigationService();
        var viewModel = new WaitlistViewDetailViewModel(
            navigationService,
            new StubSampleDataService(),
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
            WorkCenter = "100-3",
            RequestType = "Coil",
            Subtype = "Pickup Coil",
            ActiveSetupJobId = "100-3",
            WorkCenterName = "100-3",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };
        var submit = await requestService.SubmitAsync(draft, allowDuplicate: false);
        var requestId = submit.Request!.Id;

        // Empty sample rows => the item must be resolved from the request service.
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(requestId.GetHashCode());

        Assert.IsNotNull(viewModel.Item);
        Assert.AreEqual(requestId.GetHashCode(), viewModel.Item!.Id);
        Assert.IsTrue(viewModel.TemplateSections.Any(section => string.Equals(section.Title, "Coil material", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task OnNavigatedTo_CoilRequest_WorkOrderAndRequestShowsRealRequestValuesAsync()
    {
        var requestService = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "100-3",
            RequestType = "Coil",
            Subtype = "Pickup Coil",
            ActiveSetupJobId = "WO-204",
            WorkCenterName = "100-3",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };
        var submit = await requestService.SubmitAsync(draft, allowDuplicate: false);
        var requestId = submit.Request!.Id;

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(requestId.GetHashCode());

        var section = viewModel.TemplateSections.First(section =>
            string.Equals(section.Title, "Work order and request", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("WO-204", SectionField(section, "Work order"), "Work order should come from the request's active job id.");
        Assert.AreEqual("100-3", SectionField(section, "Work center"));
        Assert.AreEqual("John Koll", SectionField(section, "Requesting user"));
        Assert.AreEqual("6229", SectionField(section, "Employee number"), "Employee number should come from the requester.");
    }

    private static string? SectionField(WaitlistDetailTemplateSection section, string label)
        => section.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;

    [TestMethod]
    public void OnNavigatedTo_NoItemResolved_SetsFriendlyEmptyStateMessage()
    {
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(),   // empty sample, no request service -> nothing resolves
            new StubBuildingSelectionService());

        viewModel.OnNavigatedTo(9999);

        Assert.IsNull(viewModel.Item);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.EmptyStateMessage));
    }

    [TestMethod]
    public void OnNavigatedTo_ItemResolved_ClearsEmptyStateMessage()
    {
        var item = new SampleOrder { Id = 7, Title = "Coil Request" };
        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubSampleDataService(item),
            new StubBuildingSelectionService());

        viewModel.OnNavigatedTo(7);

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

    private sealed class StubSampleDataService : ISampleDataService
    {
        private readonly IReadOnlyList<object> _items;

        public StubSampleDataService(params SampleOrder[] items)
        {
            _items = items.Length == 0 ? Array.Empty<object>() : items.Cast<object>().ToArray();
        }

        public IReadOnlyList<object> GetSampleOrders(string? building = null)
        {
            return _items;
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