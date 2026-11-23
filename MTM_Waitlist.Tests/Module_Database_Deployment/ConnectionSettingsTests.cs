using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// What the database picker offers, and what it says about each choice.
/// </summary>
/// <remarks>
/// A deployment tool that silently retargets itself is the most dangerous kind, so the picker's list is pinned:
/// the live store, the practice copy, and the cache mirror each have to survive whatever the settings file holds.
/// </remarks>
[TestClass]
public sealed class Model_ConnectionSettingsTests
{
    [TestMethod]
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_OffersTheShippedStoresAndTheirCopies()
    {
        var settings = new Model_ConnectionSettings();

        CollectionAssert.AreEqual(
            new[]
            {
                Model_ConnectionSettings.LiveDatabase,
                Model_ConnectionSettings.TestDatabase,
                "mtm_mock",
                "mtm_mock_test"
            },
            settings.ResolveAvailableDatabases().ToArray());
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_KeepsASelectionThatIsNotListed()
    {
        var settings = new Model_ConnectionSettings
        {
            Database = "mtm_custom",
            AvailableDatabases = [Model_ConnectionSettings.LiveDatabase, Model_ConnectionSettings.TestDatabase]
        };

        var databases = settings.ResolveAvailableDatabases();

        Assert.AreEqual("mtm_custom", databases[0]);
        Assert.AreEqual(3, databases.Count);
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_DoesNotRepeatASelectionAlreadyListed()
    {
        var settings = new Model_ConnectionSettings
        {
            Database = Model_ConnectionSettings.TestDatabase,
            AvailableDatabases = [Model_ConnectionSettings.LiveDatabase, Model_ConnectionSettings.TestDatabase]
        };

        Assert.AreEqual(2, settings.ResolveAvailableDatabases().Count);
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_IgnoresBlanksAndCaseDuplicates()
    {
        var settings = new Model_ConnectionSettings
        {
            Database = string.Empty,
            AvailableDatabases = ["  ", Model_ConnectionSettings.LiveDatabase, "MTM_WAITLIST", null!]
        };

        var databases = settings.ResolveAvailableDatabases();

        Assert.AreEqual(1, databases.Count);
        Assert.AreEqual(Model_ConnectionSettings.LiveDatabase, databases[0]);
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_FallsBackToLiveWhenNothingIsConfigured()
    {
        var settings = new Model_ConnectionSettings
        {
            Database = string.Empty,
            AvailableDatabases = []
        };

        CollectionAssert.AreEqual(
            new[] { Model_ConnectionSettings.LiveDatabase },
            settings.ResolveAvailableDatabases().ToArray());
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_Clone_CopiesTheDatabaseListRatherThanSharingIt()
    {
        var settings = new Model_ConnectionSettings();
        var shipped = settings.AvailableDatabases.Count;
        var clone = settings.Clone();

        settings.AvailableDatabases.Add("mtm_added_later");

        Assert.AreEqual(shipped, clone.AvailableDatabases.Count);
        CollectionAssert.DoesNotContain(clone.AvailableDatabases, "mtm_added_later");
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_DatabaseOption_For_WarnsThatTheLiveStoreIsReal()
    {
        var option = Model_DatabaseOption.For(Model_ConnectionSettings.LiveDatabase);

        Assert.AreEqual("Live database", option.Title);
        StringAssert.Contains(option.Description, "real");
        Assert.AreEqual("Live database - mtm_waitlist", option.Display);
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_DatabaseOption_For_CallsATestNameAPracticeCopy()
    {
        var option = Model_DatabaseOption.For("mtm_waitlist_test");

        Assert.AreEqual("Test database", option.Title);
        StringAssert.Contains(option.Description, "practice copy");
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_DatabaseOption_For_CallsTheMockStoreACacheMirror()
    {
        var option = Model_DatabaseOption.For("mtm_mock");

        Assert.AreEqual("Cache mirror", option.Title);
        StringAssert.Contains(option.Description, "falls back");
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_DatabaseOption_ToString_ReadsAsThePickerShowsIt()
    {
        var option = Model_DatabaseOption.For(Model_ConnectionSettings.LiveDatabase);

        Assert.AreEqual(option.Display, option.ToString());
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_For_CallsACopyOfTheMirrorACacheMirrorCopy()
    {
        var option = Model_DatabaseOption.For("mtm_mock_test");

        Assert.AreEqual("Cache mirror copy", option.Title);
        StringAssert.Contains(option.Description, "duplicate of the Infor Visual cache");
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ForMerge_KeepsTheConfiguredOrderThenAddsTheServerNames()
    {
        var options = Model_DatabaseOption.Merge(
            (string[])["mtm_waitlist", "mtm_waitlist_test", "mtm_mock"],
            (string[])["zz_other", "mtm_mock_test", "mtm_waitlist"]);

        CollectionAssert.AreEqual(
            new[] { "mtm_waitlist", "mtm_waitlist_test", "mtm_mock", "zz_other", "mtm_mock_test" },
            options.Select(option => option.Name).ToArray());
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ForMerge_DoesNotListANameTwice()
    {
        var options = Model_DatabaseOption.Merge((string[])["mtm_mock"], (string[])["MTM_MOCK", "mtm_mock_test"]);

        CollectionAssert.AreEqual(
            new[] { "mtm_mock", "mtm_mock_test" },
            options.Select(option => option.Name).ToArray());
    }

    [TestMethod]
    public void Model_ConnectionSettingsTests_ServiceMerge_AddsAShippedNameToASettingsFileThatPredatesIt()
    {
        var merged = Service_DeploymentSettings.MergeAvailableDatabases(
            (string[])["mtm_waitlist", "mtm_mock", "mtm_mock_test"],
            (string[])["mtm_waitlist", "mtm_mock"]);

        CollectionAssert.AreEqual(
            new[] { "mtm_waitlist", "mtm_mock", "mtm_mock_test" },
            merged.ToArray());
    }
}
