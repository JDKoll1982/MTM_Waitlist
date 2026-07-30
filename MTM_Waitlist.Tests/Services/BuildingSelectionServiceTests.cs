using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class BuildingSelectionServiceTests
{
    [TestMethod]
    public void Constructor_SelectsFirstBuildingByDefault()
    {
        var service = new BuildingSelectionService();

        CollectionAssert.AreEqual(new[] { "Expo Drive", "Vits Drive" }, service.Buildings.ToArray());
        Assert.AreEqual("Expo Drive", service.SelectedBuilding);
    }

    [TestMethod]
    public void SelectedBuilding_IgnoresWhitespaceAndDuplicateAssignments()
    {
        var service = new BuildingSelectionService();
        var changeCount = 0;

        service.BuildingChanged += (_, _) => changeCount++;

        service.SelectedBuilding = "";
        service.SelectedBuilding = "Expo Drive";

        Assert.AreEqual("Expo Drive", service.SelectedBuilding);
        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SelectedBuilding_RaisesEventWhenChanged()
    {
        var service = new BuildingSelectionService();
        var changeCount = 0;

        service.BuildingChanged += (_, _) => changeCount++;

        service.SelectedBuilding = "Vits Drive";

        Assert.AreEqual("Vits Drive", service.SelectedBuilding);
        Assert.AreEqual(1, changeCount);
    }
}