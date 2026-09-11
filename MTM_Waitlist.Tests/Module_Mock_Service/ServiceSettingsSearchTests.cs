using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.ViewModels;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the search the service window's title bar drives: a group card matches on its heading, its
/// note, or the words an operator would plausibly type for the settings inside it, every typed word has to
/// match, and a search opens the cards it matched so the result is visible.
/// </summary>
/// <remarks>
/// The search box lives in the shell window and the meaning of a search lives in each surface, so this
/// covers the surface half of that split. The round trip through the window itself needs the running app.
/// </remarks>
[TestClass]
public sealed class ServiceSettingsSearchTests
{
    private const string Header = "MySQL host and backup tool";
    private const string Description = "The MySQL server that holds the application stores and the cache.";

    [TestMethod]
    public void NoSearch_KeepsEveryGroupVisible()
    {
        var group = CreateGroup();

        group.ApplySearch(string.Empty);

        Assert.IsTrue(group.IsVisible, "An empty search must not hide anything.");
    }

    [TestMethod]
    public void WhiteSpaceSearch_KeepsEveryGroupVisible()
    {
        var group = CreateGroup();

        group.ApplySearch("   ");

        Assert.IsTrue(group.IsVisible, "Whitespace is not a search.");
    }

    [TestMethod]
    public void Search_MatchesTheHeading()
    {
        var group = CreateGroup();

        group.ApplySearch("backup");

        Assert.IsTrue(group.IsVisible);
    }

    [TestMethod]
    public void Search_MatchesTheNote()
    {
        var group = CreateGroup();

        group.ApplySearch("cache");

        Assert.IsTrue(group.IsVisible);
    }

    [TestMethod]
    public void Search_MatchesAKeywordThatIsNotOnTheCard()
    {
        // "mysqldump" names the setting inside this group but appears in neither the heading nor the note,
        // which is exactly what the keyword list is for.
        var group = CreateGroup();

        group.ApplySearch("mysqldump");

        Assert.IsTrue(group.IsVisible);
    }

    [TestMethod]
    public void Search_IgnoresCase()
    {
        var group = CreateGroup();

        group.ApplySearch("MySQL");

        Assert.IsTrue(group.IsVisible);
    }

    [TestMethod]
    public void Search_RequiresEveryTypedWordToMatch()
    {
        // "mysql port" must narrow rather than widen: one word matches the heading, and "port" is only in
        // the keyword list, so the group stays; adding a word that matches nothing hides it.
        var group = CreateGroup();

        group.ApplySearch("mysql port");
        Assert.IsTrue(group.IsVisible, "Both words match this group.");

        group.ApplySearch("mysql portaardvark");
        Assert.IsFalse(group.IsVisible, "A word that matches nothing must hide the group.");
    }

    [TestMethod]
    public void Search_ThatMatchesNothing_HidesTheGroup()
    {
        var group = CreateGroup();

        group.ApplySearch("emergency restore");

        Assert.IsFalse(group.IsVisible);
    }

    [TestMethod]
    public void Search_OpensTheCardsItMatched()
    {
        var group = CreateGroup();

        group.ApplySearch("backup");

        Assert.IsTrue(group.IsExpanded, "A matched card is opened so the result is on screen.");
    }

    [TestMethod]
    public void Search_LeavesTheCardClosedWhenItDidNotMatch()
    {
        var group = CreateGroup();

        group.ApplySearch("emergency restore");

        Assert.IsFalse(group.IsExpanded);
    }

    [TestMethod]
    public void ClearingTheSearch_DoesNotCollapseOrHideTheCards()
    {
        var group = CreateGroup();
        group.IsExpanded = true;

        group.ApplySearch("backup");
        group.ApplySearch(string.Empty);

        Assert.IsTrue(group.IsVisible);
        Assert.IsTrue(group.IsExpanded, "Clearing a search restores the surface without collapsing it.");
    }

    [TestMethod]
    public void Group_CarriesItsOwnHeadingAndNote()
    {
        var group = CreateGroup();

        Assert.AreEqual(Header, group.HeaderText);
        Assert.AreEqual(Description, group.DescriptionText);
        Assert.AreEqual("mysql", group.Key);
    }

    private static ServiceSettingsGroupViewModel CreateGroup() =>
        new(
            "mysql",
            Header,
            Description,
            "mysql", "server", "port", "login", "user", "password", "file", "option", "backup", "tool",
            "mysqldump", "path");
}

