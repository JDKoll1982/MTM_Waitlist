using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

[TestClass]
public sealed class NewRequestJobTypeViewModelTests
{
    [TestMethod]
    public async Task OnNavigatedTo_JobHasNoCoil_CoilTileIsHidden()
    {
        var viewModel = new NewRequestJobTypeViewModel(
            new FakeNavigationService(),
            new FakeNewRequestFlowService(),
            new FakeCoilService(new WaitlistCoilInfo { HasCoil = false }));

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.JobTypes.Any(item => item.Name == "Pickup"));

        Assert.IsFalse(viewModel.JobTypes.Any(item => string.Equals(item.Name, "Coil", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(viewModel.JobTypes.Any(item => string.Equals(item.Name, "Pickup", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasCoil_CoilTileIsShown()
    {
        var viewModel = new NewRequestJobTypeViewModel(
            new FakeNavigationService(),
            new FakeNewRequestFlowService(),
            new FakeCoilService(new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204" }));

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.JobTypes.Any(item => item.Name == "Pickup"));

        Assert.IsTrue(viewModel.JobTypes.Any(item => string.Equals(item.Name, "Coil", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasCoil_ShowsCoilBanner()
    {
        var viewModel = new NewRequestJobTypeViewModel(
            new FakeNavigationService(),
            new FakeNewRequestFlowService(),
            new FakeCoilService(new WaitlistCoilInfo { HasCoil = true, CoilNumber = "COIL-204", QuantityOnHand = "18 coils", Description = "galvanized", AverageWeight = "1,240 lb" }));

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.JobTypes.Any(item => item.Name == "Pickup"));

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("COIL-204", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task OnNavigatedTo_JobHasNoCoil_ShowsNoCoilBanner()
    {
        var viewModel = new NewRequestJobTypeViewModel(
            new FakeNavigationService(),
            new FakeNewRequestFlowService(),
            new FakeCoilService(new WaitlistCoilInfo { HasCoil = false }));

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-17" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.JobTypes.Any(item => item.Name == "Pickup"));

        Assert.IsTrue(viewModel.IsCoilBannerVisible);
        Assert.IsTrue(viewModel.CoilBannerText.Contains("No coil", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task OnNavigatedTo_LivePlaceholderNoCoilNumber_HidesBanner()
    {
        var viewModel = new NewRequestJobTypeViewModel(
            new FakeNavigationService(),
            new FakeNewRequestFlowService(),
            new FakeCoilService(new WaitlistCoilInfo { HasCoil = true }));

        viewModel.OnNavigatedTo(new NewRequestFlowState { WorkCenter = "100-3" });

        await WaitUntilAsync(() => !viewModel.IsLoading && viewModel.JobTypes.Any(item => item.Name == "Pickup"));

        Assert.IsFalse(viewModel.IsCoilBannerVisible);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                Assert.Fail("Timed out waiting for the job type view model to finish loading.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class FakeNewRequestFlowService : INewRequestFlowService
    {
        public Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewRequestTypeDefinition>>(NewRequestFlowRules.GetDefaultTypes());

        public Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> ResolveRequestSubtypeImagePathAsync(string requestTypeName, string subtypeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, string>());
    }

    private sealed class FakeCoilService : ICoilAvailabilityService
    {
        private readonly WaitlistCoilInfo _coil;

        public FakeCoilService(WaitlistCoilInfo coil)
        {
            _coil = coil;
        }

        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(_coil);
    }
}
