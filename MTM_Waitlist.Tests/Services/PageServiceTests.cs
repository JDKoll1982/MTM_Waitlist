using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.ViewModels;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Views;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class PageServiceTests
{
    [TestMethod]
    public void GetPageType_ReturnsConfiguredPageTypes()
    {
        var service = new PageService();

        Assert.AreEqual(typeof(SettingsPage), service.GetPageType(typeof(SettingsViewModel).FullName!));
        Assert.AreEqual(typeof(WaitlistViewPage), service.GetPageType(typeof(WaitlistViewViewModel).FullName!));
        Assert.AreEqual(typeof(WaitlistViewDetailPage), service.GetPageType(typeof(WaitlistViewDetailViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestWorkCenterPage), service.GetPageType(typeof(NewRequestWorkCenterViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestJobTypePage), service.GetPageType(typeof(NewRequestJobTypeViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestSubtypePage), service.GetPageType(typeof(NewRequestSubtypeViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestDetailsPage), service.GetPageType(typeof(NewRequestDetailsViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestPreviewPage), service.GetPageType(typeof(NewRequestPreviewViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestSummaryPage), service.GetPageType(typeof(NewRequestSummaryViewModel).FullName!));
        Assert.AreEqual(typeof(NewRequestResultPage), service.GetPageType(typeof(NewRequestResultViewModel).FullName!));
    }

    [TestMethod]
    public void GetPageType_ThrowsForUnknownKey()
    {
        var service = new PageService();

        Assert.ThrowsException<ArgumentException>(() => service.GetPageType("Missing.ViewModel"));
    }
}