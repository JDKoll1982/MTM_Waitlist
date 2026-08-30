using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class PageServiceTests
{
    private sealed class TestViewModelA
    {
    }

    private sealed class TestViewModelB
    {
    }

    private sealed class TestPageA
    {
    }

    private sealed class TestPageB
    {
    }

    [TestMethod]
    public void GetPageType_ReturnsConfiguredPage()
    {
        var service = new PageService();
        service.Configure<TestViewModelA, TestPageA>();

        var pageType = service.GetPageType(typeof(TestViewModelA).FullName!);

        Assert.AreEqual(typeof(TestPageA), pageType);
    }

    [TestMethod]
    public void GetPageType_ThrowsWhenNotConfigured()
    {
        var service = new PageService();

        var ex = Assert.ThrowsException<ArgumentException>(() => service.GetPageType("Missing.ViewModel"));

        StringAssert.Contains(ex.Message, "Page not found");
    }

    [TestMethod]
    public void Configure_ThrowsOnDuplicateKey()
    {
        var service = new PageService();
        service.Configure<TestViewModelA, TestPageA>();

        var ex = Assert.ThrowsException<ArgumentException>(() => service.Configure<TestViewModelA, TestPageB>());

        StringAssert.Contains(ex.Message, "already configured");
    }

    [TestMethod]
    public void Configure_ThrowsWhenPageTypeAlreadyMapped()
    {
        var service = new PageService();
        service.Configure<TestViewModelA, TestPageA>();

        var ex = Assert.ThrowsException<ArgumentException>(() => service.Configure<TestViewModelB, TestPageA>());

        StringAssert.Contains(ex.Message, "already configured");
    }

    [TestMethod]
    public void Configure_MultipleDistinctMappingsResolveIndependently()
    {
        var service = new PageService();
        service.Configure<TestViewModelA, TestPageA>();
        service.Configure<TestViewModelB, TestPageB>();

        Assert.AreEqual(typeof(TestPageA), service.GetPageType(typeof(TestViewModelA).FullName!));
        Assert.AreEqual(typeof(TestPageB), service.GetPageType(typeof(TestViewModelB).FullName!));
    }
}
