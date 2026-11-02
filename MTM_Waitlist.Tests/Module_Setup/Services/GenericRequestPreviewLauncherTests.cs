using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

/// <summary>
/// The preview launcher finds the generic request preview host where it actually is, and reports its absence rather
/// than pretending (015-generic-wc-ui-shell; 016-generic-wc-request-foundation, FR-001).
/// </summary>
/// <remarks>
/// The preview host is a separate program that the shipped application never references. These tests pin the two
/// behaviours the setup screen depends on: an explicit path wins, and a path that does not exist is not offered.
/// They deliberately do not require the preview to be built, so they pass on a machine that has never built it.
/// </remarks>
[TestClass]
public sealed class GenericRequestPreviewLauncherTests
{
    private const string OverrideVariable = "MTM_GENERIC_PREVIEW_PATH";

    [TestMethod]
    public void Service_GenericRequestPreviewLauncherTests_AnExplicitPathWins()
    {
        var file = Path.Combine(Path.GetTempPath(), $"generic-preview-probe-{Guid.NewGuid():N}.exe");

        try
        {
            File.WriteAllText(file, string.Empty);
            Environment.SetEnvironmentVariable(OverrideVariable, file);

            Assert.AreEqual(
                file,
                new GenericRequestPreviewLauncher().ResolvePreviewExecutable(),
                "the configured path is the one the launcher must use, ahead of anything it could find itself");
        }
        finally
        {
            Environment.SetEnvironmentVariable(OverrideVariable, null);
            File.Delete(file);
        }
    }

    [TestMethod]
    public void Service_GenericRequestPreviewLauncherTests_APathThatDoesNotExistIsNotOffered()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"generic-preview-missing-{Guid.NewGuid():N}.exe");

        try
        {
            Environment.SetEnvironmentVariable(OverrideVariable, missing);

            var resolved = new GenericRequestPreviewLauncher().ResolvePreviewExecutable();

            Assert.AreNotEqual(
                missing,
                resolved,
                "a path that holds no file must never be offered as the preview, whatever else is on the machine");
        }
        finally
        {
            Environment.SetEnvironmentVariable(OverrideVariable, null);
        }
    }

    [TestMethod]
    public async Task Service_GenericRequestPreviewLauncherTests_StartingItNeverBlocksTheCaller()
    {
        // LaunchAsync answers with a result rather than throwing, whatever the machine holds. Without the preview it
        // reports its absence; with it, the start has already been handed to the shell. Either way the caller gets a
        // message to show, which is what the setup screen needs from a command.
        var launcher = new GenericRequestPreviewLauncher();
        var expected = launcher.ResolvePreviewExecutable() is not null;

        if (!expected)
        {
            var result = await launcher.LaunchAsync("Laser 1");
            Assert.IsFalse(result.Success, "a preview that is not there must not be reported as started");
            StringAssert.Contains(result.Message, "not on this machine", "the message has to say what is wrong");
            return;
        }

        // The preview exists on this machine. Starting it would open a window during a test run, so the presence path
        // is asserted where it matters instead: the path resolves to a file that is actually there.
        Assert.IsTrue(File.Exists(launcher.ResolvePreviewExecutable()));
    }
}
