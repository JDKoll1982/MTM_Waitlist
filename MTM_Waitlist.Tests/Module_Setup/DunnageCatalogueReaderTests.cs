using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Tests.Module_Setup;

/// <summary>
/// The one read behind both dunnage surfaces: it projects the Setup row onto the shared one and asks the workflow
/// service exactly once.
/// </summary>
[TestClass]
public sealed class DunnageCatalogueReaderTests
{
    [TestMethod]
    public async Task Service_DunnageCatalogueReaderTests_EveryFieldTheSharedRowDeclaresIsProjected()
    {
        var workflow = new RecordingDunnageWorkflowService
        {
            Parts =
            [
                new SetupDunnagePart
                {
                    Id = "DNG-PART-01",
                    TypeId = "DNG-TYPE-01",
                    PartNumber = "DNG0007788",
                    DisplayName = "Rack, 24 x 36 wire",
                    DunnageTypeName = "Rack",
                    HomeLocation = "DUNNAGE-ROW-3",
                    ImagePath = "Parts/Rack.png",
                    Metadata = "not carried",
                    IsSelectedForPair = true,
                },
            ],
        };

        var parts = await new DunnageCatalogueReader(workflow).ReadAllAsync();

        Assert.AreEqual(1, parts.Count);
        var part = parts[0];
        Assert.AreEqual("DNG-PART-01", part.Id);
        Assert.AreEqual("DNG-TYPE-01", part.TypeId);
        Assert.AreEqual("DNG0007788", part.PartNumber);
        Assert.AreEqual("Rack, 24 x 36 wire", part.DisplayName);
        Assert.AreEqual("Rack", part.DunnageTypeName);
        Assert.AreEqual("DUNNAGE-ROW-3", part.HomeLocation, "The one-box filter matches the home location.");
        Assert.AreEqual("Parts/Rack.png", part.ImagePath);
    }

    [TestMethod]
    public void Model_DunnageCatalogueReaderTests_TheSharedRowCarriesNothingTheSetupFlowAloneUses()
    {
        var names = typeof(DunnageCataloguePart).GetProperties().Select(property => property.Name).ToArray();

        CollectionAssert.DoesNotContain(names, nameof(SetupDunnagePart.Metadata));
        CollectionAssert.DoesNotContain(names, nameof(SetupDunnagePart.IsSelectedForPair));
    }

    [TestMethod]
    public async Task Service_DunnageCatalogueReaderTests_OneCallReadsTheWholeCatalogue()
    {
        var workflow = new RecordingDunnageWorkflowService { Parts = [new SetupDunnagePart { Id = "A" }] };

        await new DunnageCatalogueReader(workflow).ReadAllAsync();

        Assert.AreEqual(1, workflow.Calls, "The catalogue is read once, not once per surface.");
    }

    /// <summary>The Setup workflow, answering a fixed list and counting its calls.</summary>
    private sealed class RecordingDunnageWorkflowService : IDunnageWorkflowService
    {
        public IReadOnlyList<SetupDunnagePart> Parts { get; set; } = Array.Empty<SetupDunnagePart>();

        public int Calls { get; private set; }

        public Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesAsync(
            string partNumber,
            string sequenceNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupDunnageType>>(Array.Empty<SetupDunnageType>());

        public Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsAsync(
            string dunnageTypeId,
            string partNumber,
            string sequenceNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SetupDunnagePart>>(Array.Empty<SetupDunnagePart>());

        public Task<IReadOnlyList<SetupDunnagePart>> GetAllDunnagePartsAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Parts);
        }

        public Task<SetupSelectionResult> AddDunnageTypeAsync(string typeName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult());

        public Task<SetupSelectionResult> AddDunnagePartAsync(
            string dunnageTypeId,
            string partName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetupSelectionResult());
    }
}
