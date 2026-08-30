using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class PageServiceTests
{
    private sealed class FakeViewModel { }

    private sealed class FakePage { }

    private sealed class AnotherViewModel { }

    private sealed class AnotherPage { }

    [TestMethod]
    public void GetPageType_ReturnsConfiguredPageTypes()
    {
        var service = new PageService();
        service.Configure<FakeViewModel, FakePage>();
        service.Configure<AnotherViewModel, AnotherPage>();

        Assert.AreEqual(typeof(FakePage), service.GetPageType(typeof(FakeViewModel).FullName!));
        Assert.AreEqual(typeof(AnotherPage), service.GetPageType(typeof(AnotherViewModel).FullName!));
    }

    [TestMethod]
    public void GetPageType_ThrowsForUnknownKey()
    {
        var service = new PageService();

        Assert.ThrowsException<ArgumentException>(() => service.GetPageType("Missing.ViewModel"));
    }
}