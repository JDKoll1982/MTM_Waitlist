using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Services;

/// <summary>
/// The one spelling of "every building", and how it is turned back into a filter: a list asks the store for
/// nothing when it is in effect, and nothing is ever written with it as a building.
/// </summary>
[TestClass]
public sealed class BuildingScopeTests
{
    [TestMethod]
    public void Service_BuildingScopeTests_IsAllBuildings_IgnoresCaseAndSurroundingSpace()
    {
        Assert.IsTrue(BuildingScope.IsAllBuildings(BuildingScope.AllBuildings));
        Assert.IsTrue(BuildingScope.IsAllBuildings(" all buildings "));
        Assert.IsFalse(BuildingScope.IsAllBuildings("Expo Drive"));
        Assert.IsFalse(BuildingScope.IsAllBuildings(null));
        Assert.IsFalse(BuildingScope.IsAllBuildings(string.Empty));
    }

    [TestMethod]
    public void Service_BuildingScopeTests_AsFilter_AnswersNothingForEveryBuildingAndABlankChoice()
    {
        Assert.IsNull(BuildingScope.AsFilter(BuildingScope.AllBuildings));
        Assert.IsNull(BuildingScope.AsFilter(null));
        Assert.IsNull(BuildingScope.AsFilter("  "));
    }

    [TestMethod]
    public void Service_BuildingScopeTests_AsFilter_AnswersTheBuildingItselfOtherwise()
    {
        Assert.AreEqual("Expo Drive", BuildingScope.AsFilter("  Expo Drive  "));
        Assert.AreEqual("Vits Drive", BuildingScope.AsFilter("Vits Drive"));
    }
}
