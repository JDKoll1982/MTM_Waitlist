using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The handler-condition vocabulary: four stored strings, verbatim, in one order, and nothing else is a
/// condition. The card states a condition only when the row actually carries one, so a value the store never
/// wrote must not be shown as one.
/// </summary>
[TestClass]
public sealed class HandlerStatusCatalogTests
{
    [TestMethod]
    public void Helper_HandlerStatusCatalogTests_TheFourConditionsAreTheStoredStringsVerbatimInTheirSettledOrder()
    {
        CollectionAssert.AreEqual(
            new[] { "Product Buried", "Cant Find Product", "Changing Forklift", "Changing Propane" },
            HandlerStatusCatalog.Conditions.ToArray(),
            "The four conditions are pinned by the spec and are used exactly, in this order (FR-071, FR-102).");
    }

    [TestMethod]
    public void Helper_HandlerStatusCatalogTests_EachOfTheFourConditionsIsRecognised()
    {
        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            Assert.IsTrue(
                HandlerStatusCatalog.IsCondition(condition),
                $"'{condition}' is one of the four conditions and must be recognised.");
        }
    }

    [TestMethod]
    public void Helper_HandlerStatusCatalogTests_AValueThatIsNotOneOfTheFourIsNotACondition()
    {
        // A fifth value, a lifecycle word, and near misses of a real condition: the vocabulary is closed and
        // the comparison is verbatim, so none of these may be shown as a condition the handler reported.
        foreach (var rejected in new[]
                 {
                     "Product Missing",
                     "Product Buried ",
                     "Product buried",
                     "Pending",
                     "Accepted",
                 })
        {
            Assert.IsFalse(
                HandlerStatusCatalog.IsCondition(rejected),
                $"'{rejected}' is not one of the four stored conditions, so the status word must fall through.");
        }
    }

    [TestMethod]
    public void Helper_HandlerStatusCatalogTests_NullAndWhitespaceAreNotConditions()
    {
        foreach (var rejected in new[] { null, "", "   " })
        {
            Assert.IsFalse(
                HandlerStatusCatalog.IsCondition(rejected),
                $"'{rejected ?? "null"}' is an unset condition, not a reported one.");
        }
    }

    [TestMethod]
    public void Helper_HandlerStatusCatalogTests_TheConditionListIsReadOnly()
    {
        // The list is the vocabulary itself, so a caller must not be able to add a fifth value to it.
        var asMutableList = HandlerStatusCatalog.Conditions as IList<string>;

        Assert.IsTrue(
            asMutableList is null || asMutableList.IsReadOnly,
            "The condition list must not be a mutable collection a caller could add a fifth condition to.");
    }
}
