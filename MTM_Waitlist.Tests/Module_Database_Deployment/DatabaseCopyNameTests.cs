using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// The name the operator types for a copy reaches a statement, so it is checked before anything is created.
/// </summary>
[TestClass]
public sealed class DatabaseCopyNameTests
{
    [TestMethod]
    public void Service_DatabaseCopyNameTests_EnsureCopyNameIsUsable_AcceptsALettersDigitsUnderscoresName()
    {
        DatabaseBackup.EnsureCopyNameIsUsable("mtm_mock", "mtm_mock_test");
    }

    [TestMethod]
    public void Service_DatabaseCopyNameTests_EnsureCopyNameIsUsable_RefusesTheSourceItself()
    {
        var thrown = Assert.ThrowsException<ArgumentException>(
            () => DatabaseBackup.EnsureCopyNameIsUsable("mtm_mock", "MTM_MOCK"));

        StringAssert.Contains(thrown.Message, "different name");
    }

    [TestMethod]
    public void Service_DatabaseCopyNameTests_EnsureCopyNameIsUsable_RefusesAnEmptyName()
    {
        var thrown = Assert.ThrowsException<ArgumentException>(
            () => DatabaseBackup.EnsureCopyNameIsUsable("mtm_mock", "  "));

        StringAssert.Contains(thrown.Message, "Type a name");
    }

    [TestMethod]
    public void Service_DatabaseCopyNameTests_EnsureCopyNameIsUsable_RefusesANameCarryingAStatement()
    {
        var thrown = Assert.ThrowsException<ArgumentException>(
            () => DatabaseBackup.EnsureCopyNameIsUsable("mtm_mock", "mtm_mock_test; DROP DATABASE mtm_waitlist"));

        StringAssert.Contains(thrown.Message, "letters, numbers and underscores");
    }

    [TestMethod]
    public void Service_DatabaseCopyNameTests_EnsureCopyNameIsUsable_RefusesANameLongerThanSixtyFourCharacters()
    {
        var thrown = Assert.ThrowsException<ArgumentException>(
            () => DatabaseBackup.EnsureCopyNameIsUsable("mtm_mock", new string('a', 65)));

        StringAssert.Contains(thrown.Message, "64 characters");
    }
}
