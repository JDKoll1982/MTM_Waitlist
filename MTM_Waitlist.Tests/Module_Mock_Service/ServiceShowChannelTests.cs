using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the channel a second launch uses to ask the running service to show a surface.
/// </summary>
/// <remarks>
/// The names are only ever used inside the service - a second launch signals them, the running service
/// waits on them - so the point of pinning them here is that a rename cannot silently leave the desktop
/// shortcut's "Show UI" doing nothing against an already-running service.
/// </remarks>
[TestClass]
public sealed class ServiceShowChannelTests
{
    [TestMethod]
    public void EventNames_AreSessionLocal()
    {
        StringAssert.StartsWith(
            ServiceShowChannel.StatusEventName,
            @"Local\",
            "The channel is per session: a service in another session cannot show a window on this desktop.");

        StringAssert.StartsWith(ServiceShowChannel.SettingsEventName, @"Local\");
    }

    [TestMethod]
    public void EventNames_AreDistinct()
    {
        Assert.AreNotEqual(
            ServiceShowChannel.StatusEventName,
            ServiceShowChannel.SettingsEventName,
            "Status and settings requests must not be able to reach each other's event.");
    }

    [TestMethod]
    public void EventNames_AreNamespacedToThisService()
    {
        StringAssert.Contains(ServiceShowChannel.StatusEventName, "MTM_Waitlist.Mock.Service");
        StringAssert.Contains(ServiceShowChannel.SettingsEventName, "MTM_Waitlist.Mock.Service");
    }
}
