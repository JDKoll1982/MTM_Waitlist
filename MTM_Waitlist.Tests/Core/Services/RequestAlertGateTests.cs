using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class RequestAlertGateTests
{
    [TestMethod]
    public void ShouldShowToast_OnlyWhenEnabledAndPackaged()
    {
        Assert.IsTrue(RequestAlertGate.ShouldShowToast(alertsEnabled: true, isPackaged: true));
        Assert.IsFalse(RequestAlertGate.ShouldShowToast(alertsEnabled: true, isPackaged: false));
        Assert.IsFalse(RequestAlertGate.ShouldShowToast(alertsEnabled: false, isPackaged: true));
        Assert.IsFalse(RequestAlertGate.ShouldShowToast(alertsEnabled: false, isPackaged: false));
    }

    [TestMethod]
    public void ShouldNotifyOnCreated_RequiresSignalAndEnabledAndPackaged()
    {
        Assert.IsTrue(RequestAlertGate.ShouldNotifyOnCreated(requestCreatedSignal: true, alertsEnabled: true, isPackaged: true));
        Assert.IsFalse(RequestAlertGate.ShouldNotifyOnCreated(requestCreatedSignal: false, alertsEnabled: true, isPackaged: true));
        Assert.IsFalse(RequestAlertGate.ShouldNotifyOnCreated(requestCreatedSignal: true, alertsEnabled: true, isPackaged: false));
    }
}
