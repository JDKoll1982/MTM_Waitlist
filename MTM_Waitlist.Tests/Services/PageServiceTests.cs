using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Services;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Views;

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
    }

    [TestMethod]
    public void GetPageType_ThrowsForUnknownKey()
    {
        var service = new PageService();

        Assert.ThrowsException<ArgumentException>(() => service.GetPageType("Missing.ViewModel"));
    }
}