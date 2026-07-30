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

        Assert.AreEqual(typeof(LoginPage), service.GetPageType(typeof(LoginViewModel).FullName!));
        Assert.AreEqual(typeof(SettingsPage), service.GetPageType(typeof(SettingsViewModel).FullName!));
        Assert.AreEqual(typeof(DeveloperModePage), service.GetPageType(typeof(DeveloperModeViewModel).FullName!));
        Assert.AreEqual(typeof(WaitlistViewPage), service.GetPageType(typeof(WaitlistViewViewModel).FullName!));
        Assert.AreEqual(typeof(WaitlistViewDetailPage), service.GetPageType(typeof(WaitlistViewDetailViewModel).FullName!));
    }

    [TestMethod]
    public void GetPageType_ThrowsForUnknownKey()
    {
        var service = new PageService();

        Assert.ThrowsException<ArgumentException>(() => service.GetPageType("Missing.ViewModel"));
    }
}