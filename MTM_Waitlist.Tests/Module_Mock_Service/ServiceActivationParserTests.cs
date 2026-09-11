using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies how a launch of the service is interpreted: only an explicit switch opens a window, and
/// anything else stays tray-only.
/// </summary>
/// <remarks>
/// The distinction is load-bearing. Logon auto-start launches the service with no arguments, and the
/// deployment health check asserts that such a launch produces <b>no top-level window</b>; the desktop
/// shortcut's "Show UI" is the one thing that should produce one.
/// </remarks>
[TestClass]
public sealed class ServiceActivationParserTests
{
    [TestMethod]
    public void NoArguments_RequestsNothing()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.None,
            ServiceActivationParser.Parse(null));

        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.None,
            ServiceActivationParser.Parse(string.Empty));

        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.None,
            ServiceActivationParser.Parse("   "),
            "Whitespace only is still a bare launch, so the service must stay tray-only.");
    }

    [TestMethod]
    public void StatusSwitch_RequestsTheStatusSurface()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Status,
            ServiceActivationParser.Parse("--open-status"));
    }

    [TestMethod]
    public void SettingsSwitch_RequestsTheSettingsSurface()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Settings,
            ServiceActivationParser.Parse("--open-settings"));
    }

    [TestMethod]
    public void Switch_IsCaseInsensitive()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Status,
            ServiceActivationParser.Parse("--OPEN-STATUS"));
    }

    [TestMethod]
    public void Switch_IsFoundBesideOtherArguments()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Settings,
            ServiceActivationParser.Parse("--some-other-flag --open-settings --and-another"));
    }

    [TestMethod]
    public void QuotedSwitch_IsRecognized()
    {
        // A shell may pass the switch as a quoted token.
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Status,
            ServiceActivationParser.Parse("\"--open-status\""));
    }

    [TestMethod]
    public void UnknownArguments_RequestNothing()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.None,
            ServiceActivationParser.Parse("--open-everything --status"),
            "Only the exact switches may open a window; a near miss must not.");
    }

    [TestMethod]
    public void SwitchNamesAreStable()
    {
        // The desktop shortcut carries these strings in its arguments, so renaming one silently would
        // leave the shortcut doing nothing at all.
        Assert.AreEqual("--open-status", ServiceActivationParser.StatusSwitch);
        Assert.AreEqual("--open-settings", ServiceActivationParser.SettingsSwitch);
    }

    [TestMethod]
    public void ProcessArguments_FindTheSwitchBesideTheExecutablePath()
    {
        // This is the path an unpackaged launch actually takes: the WinUI launch arguments do not
        // reliably carry the command line, so the process command line is read instead.
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.Status,
            ServiceActivationParser.ParseTokens(
            [
                @"C:\Services\MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.exe",
                "--open-status"
            ]));
    }

    [TestMethod]
    public void ProcessArguments_WithoutASwitchRequestNothing()
    {
        Assert.AreEqual(
            ServiceActivationParser.RequestedSurface.None,
            ServiceActivationParser.ParseTokens(
            [
                @"C:\Services\MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.exe"
            ]),
            "The path alone must never open a window: that is the logon auto-start case.");
    }
}
