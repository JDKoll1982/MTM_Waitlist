using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Tests.Core.Helpers;

[TestClass]
public sealed class StartupDebugLogTests
{
    private sealed class RecordingLogService : IStartupLogService
    {
        public List<(string Area, string Message)> InfoCalls { get; } = new();

        public List<(string Area, Exception? Exception, string Message)> ErrorCalls { get; } = new();

        public void Info(string area, string message) => InfoCalls.Add((area, message));

        public void Error(string area, Exception? exception, string message) => ErrorCalls.Add((area, exception, message));
    }

    [TestCleanup]
    public void Cleanup()
    {
        StartupDebugLog.Configure(null);
    }

    [TestMethod]
    public void Configure_WithService_InfoForwardsToService()
    {
        var service = new RecordingLogService();
        StartupDebugLog.Configure(service);

        StartupDebugLog.Info("Area", "hello");

        Assert.AreEqual(1, service.InfoCalls.Count);
        Assert.AreEqual("Area", service.InfoCalls[0].Area);
        Assert.AreEqual("hello", service.InfoCalls[0].Message);
    }

    [TestMethod]
    public void Configure_WithService_ErrorForwardsToService()
    {
        var service = new RecordingLogService();
        StartupDebugLog.Configure(service);
        var exception = new InvalidOperationException("boom");

        StartupDebugLog.Error("Area", exception, "failed");

        Assert.AreEqual(1, service.ErrorCalls.Count);
        Assert.AreEqual("Area", service.ErrorCalls[0].Area);
        Assert.AreSame(exception, service.ErrorCalls[0].Exception);
        Assert.AreEqual("failed", service.ErrorCalls[0].Message);
    }

    [TestMethod]
    public void Info_WithoutService_DoesNotThrow()
    {
        StartupDebugLog.Configure(null);

        StartupDebugLog.Info("Area", "no-op");
    }
}
