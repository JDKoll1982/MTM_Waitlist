using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;

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
    public void Model_ConnectionSettingsTests_ResolveAvailableDatabases_OffersTheThreeShippedStores()
    {
        var settings = new Model_ConnectionSettings();

        CollectionAssert.AreEqual(
            new[] { Model_ConnectionSettings.LiveDatabase, Model_ConnectionSettings.TestDatabase, "mtm_mock" },
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
        var clone = settings.Clone();

        settings.AvailableDatabases.Add("mtm_added_later");

        Assert.AreEqual(3, clone.AvailableDatabases.Count);
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
}
