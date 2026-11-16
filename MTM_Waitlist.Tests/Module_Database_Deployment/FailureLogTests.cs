using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// The note left behind when the tool dies before its window opens.
/// </summary>
/// <remarks>
/// The tool's job is to touch a live store, so a crash has to leave the operator something to hand over. These
/// cases pin that the note names where it happened, unwraps the real cause, and never carries the password.
/// </remarks>
[TestClass]
public sealed class Service_FailureLogTests
{
    [TestMethod]
    public void Service_FailureLogTests_Describe_NamesTheSourceAndTheExecutableFolder()
    {
        var text = Service_FailureLog.Describe("Startup", new InvalidOperationException("boom"));

        StringAssert.Contains(text, "Where: Startup");
        StringAssert.Contains(text, "Executable: " + AppContext.BaseDirectory);
    }

    [TestMethod]
    public void Service_FailureLogTests_Describe_UnwrapsEveryInnerExceptionSoTheRealCauseIsVisible()
    {
        var exception = new InvalidOperationException(
            "outer",
            new TypeInitializationException("Some.Type", new DllNotFoundException("missing.dll")));

        var text = Service_FailureLog.Describe("Startup", exception);

        StringAssert.Contains(text, "outer");
        StringAssert.Contains(text, "Some.Type");
        StringAssert.Contains(text, "missing.dll");
    }

    [TestMethod]
    public void Service_FailureLogTests_Describe_CarriesTheCallersContext()
    {
        var text = Service_FailureLog.Describe(
            "Startup",
            new InvalidOperationException("boom"),
            "Database: mtm_waitlist User: root");

        StringAssert.Contains(text, "Database: mtm_waitlist");
    }

    [TestMethod]
    public void Service_FailureLogTests_Write_PutsTheFileUnderTheToolFolderAndItReadsBack()
    {
        var path = Service_FailureLog.Write("Test", new InvalidOperationException("written-by-test"));

        try
        {
            Assert.IsNotNull(path);
            Assert.IsTrue(File.Exists(path));
            StringAssert.Contains(File.ReadAllText(path), "written-by-test");
            StringAssert.StartsWith(path, Service_FailureLog.LogDirectory);
        }
        finally
        {
            if (path is not null && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
