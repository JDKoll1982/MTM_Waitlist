using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Module_GenericRequest;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// SC-003 proved by test rather than by inspection: the generic page and the substitute dialog read one catalogue
/// through one implementation.
/// </summary>
[TestClass]
public sealed class DunnageCatalogueParityTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Model_DunnageCatalogueParityTests_TheSeamHasOneImplementationOverTheSetupWorkflow()
    {
        Assert.IsTrue(
            typeof(IDunnageCatalogueReader).IsAssignableFrom(typeof(DunnageCatalogueReader)),
            "the reader both surfaces draw is not the Setup implementation, so a second read has appeared.");

        Assert.AreEqual(
            "MTM_Waitlist.Setup",
            typeof(DunnageCatalogueReader).Assembly.GetName().Name,
            "the catalogue read no longer lives beside the workflow service it delegates to.");

        var constructors = typeof(DunnageCatalogueReader).GetConstructors();
        Assert.AreEqual(1, constructors.Length, "the reader has more than one way to be built.");
        CollectionAssert.AreEqual(
            new[] { typeof(IDunnageWorkflowService) },
            constructors[0].GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
            "the reader no longer reads through the workflow service, so it has found a second source.");
    }

    [TestMethod]
    public void Model_DunnageCatalogueParityTests_TheCompositionRootBindsTheSeamToThatImplementation()
    {
        var registration = File.ReadAllText(Path.Combine(
            Root,
            "Services",
            "DependencyInjection",
            "ServiceRegistrationExtensions.DunnageCatalogue.cs"));

        StringAssert.Contains(
            registration,
            "IDunnageCatalogueReader, MTM_Waitlist.Module_Setup.Services.DunnageCatalogueReader",
            "the interface is not bound to the Setup reader, so resolving it would fail or reach something else.");
    }

    [TestMethod]
    public void Model_DunnageCatalogueParityTests_BothSurfacesHoldTheOneSharedViewModel()
    {
        Assert.AreEqual(
            typeof(DunnageCatalogueViewModel),
            typeof(GenericRequestDunnageViewModel).GetProperty(nameof(GenericRequestDunnageViewModel.Catalogue))?.PropertyType,
            "the generic page is not drawing the shared catalogue, so it has its own list.");

        Assert.AreEqual(
            typeof(DunnageCatalogueViewModel),
            typeof(SetupDunnageImageSearchDialogViewModel).GetProperty(nameof(SetupDunnageImageSearchDialogViewModel.Catalogue))?.PropertyType,
            "the substitute dialog is not drawing the shared catalogue, so it has its own list.");
    }

    [TestMethod]
    public async Task Service_DunnageCatalogueParityTests_APartAddedToTheSourceAppearsOnBothAndARemovedOneOnNeither()
    {
        var catalogue = new FakeDunnageCatalogueReader
        {
            Parts = [FakeDunnageCataloguePart.Of("DNG-1")],
        };

        Assert.IsTrue(await AppearsOnBothAsync(catalogue, "DNG-1"), "a part the source holds is offered by both surfaces.");

        catalogue.Parts = [FakeDunnageCataloguePart.Of("DNG-1"), FakeDunnageCataloguePart.Of("DNG-2")];
        Assert.IsTrue(await AppearsOnBothAsync(catalogue, "DNG-2"), "a part added to the source appears on both surfaces.");

        catalogue.Parts = [FakeDunnageCataloguePart.Of("DNG-2")];
        Assert.IsFalse(await AppearsOnBothAsync(catalogue, "DNG-1"), "a part removed from the source is offered by neither.");
    }

    private static async Task<bool> AppearsOnBothAsync(FakeDunnageCatalogueReader catalogue, string partNumber)
    {
        var preferences = new InMemoryScopedPreferenceStore();

        var pageSide = new GenericRequestDunnageViewModel(
            new DunnageCatalogueViewModel(catalogue, preferences),
            new StubGenericRequestNavigator());
        await pageSide.LoadAsync();

        var dialogSide = new SetupDunnageImageSearchDialogViewModel(catalogue, preferences);
        await dialogSide.InitializeAsync();

        var onThePage = pageSide.Catalogue.DisplayedParts.Any(part => part.PartNumber == partNumber);
        var inTheDialog = dialogSide.DisplayedParts.Any(part => part.PartNumber == partNumber);

        Assert.AreEqual(onThePage, inTheDialog, $"'{partNumber}' is offered by one surface and not the other.");

        return onThePage;
    }
}
