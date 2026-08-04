using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;
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