using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class BuildingSelectionServiceTests
{
    [TestMethod]
    public void Service_BuildingSelectionServiceTests_Constructor_SelectsFirstBuildingByDefault()
    {
        var service = new BuildingSelectionService();

        CollectionAssert.AreEqual(new[] { "Expo Drive", "Vits Drive" }, service.Buildings.ToArray());
        Assert.AreEqual("Expo Drive", service.SelectedBuilding);
    }

    [TestMethod]
    public void Service_BuildingSelectionServiceTests_SelectedBuilding_IgnoresWhitespaceAndDuplicateAssignments()
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
    public void Service_BuildingSelectionServiceTests_SelectedBuilding_RaisesEventWhenChanged()
    {
        var service = new BuildingSelectionService();
        var changeCount = 0;

        service.BuildingChanged += (_, _) => changeCount++;

        service.SelectedBuilding = "Vits Drive";

        Assert.AreEqual("Vits Drive", service.SelectedBuilding);
        Assert.AreEqual(1, changeCount);
    }

    [TestMethod]
    public async Task Service_BuildingSelectionServiceTests_Initialize_StartsOnTheBuildingThisComputerWasSetUpIn()
    {
        var catalog = new FakeBuildingCatalogService();
        catalog.ComputerBuildings["test-workstation"] = "Vits Drive";

        var service = new BuildingSelectionService(catalog);
        await service.InitializeAsync();

        CollectionAssert.AreEqual(new[] { "Expo Drive", "Vits Drive" }, service.Buildings.ToArray());
        Assert.AreEqual(
            "Vits Drive",
            service.SelectedBuilding,
            "The building chosen when the workstation was set up is the one the session opens on.");
    }

    [TestMethod]
    public async Task Service_BuildingSelectionServiceTests_Initialize_FallsBackToTheFirstBuildingWhenTheComputerHoldsNone()
    {
        var service = new BuildingSelectionService(new FakeBuildingCatalogService());

        await service.InitializeAsync();

        Assert.AreEqual("Expo Drive", service.SelectedBuilding, "A computer with no answer opens on the plant's first building.");
    }

    [TestMethod]
    public async Task Service_BuildingSelectionServiceTests_Initialize_DoesNotOverwriteAChoiceAlreadyMade()
    {
        var catalog = new FakeBuildingCatalogService();
        catalog.ComputerBuildings["test-workstation"] = "Expo Drive";

        var service = new BuildingSelectionService(catalog);
        service.SelectedBuilding = "Vits Drive";

        await service.InitializeAsync();

        Assert.AreEqual("Vits Drive", service.SelectedBuilding, "A choice a person made is theirs, whatever the store says.");
    }

    [TestMethod]
    public void Service_BuildingSelectionServiceTests_SelectedBuilding_AcceptsTheEveryBuildingChoice()
    {
        var service = new BuildingSelectionService();
        var changeCount = 0;

        service.BuildingChanged += (_, _) => changeCount++;

        service.SelectedBuilding = BuildingScope.AllBuildings;

        Assert.AreEqual(BuildingScope.AllBuildings, service.SelectedBuilding);
        Assert.AreEqual(1, changeCount);
    }
}