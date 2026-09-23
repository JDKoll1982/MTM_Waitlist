using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the missing-picture list (FR-001, FR-026): the parts the application can name, minus the rows the picture
/// store already holds for that system, and the shape of the rows the export writes.
/// </summary>
[TestClass]
public sealed class PartPictureCoverageServiceTests
{
    private FakeImageOverrideReadService _readService = null!;

    [TestInitialize]
    public void TestInitialize() => _readService = new FakeImageOverrideReadService();

    [TestMethod]
    public async Task TheListIsTheNamedPartsMinusThePicturedOnes()
    {
        _readService.AddOverride("wip_part", "MMC0001000", "WIP/MMC/MMC0001000.png");

        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000", "MMC0002000"));

        var missing = await service.GetMissingPartsAsync();

        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual("MMC0002000", missing[0].PartNumber);
        Assert.AreEqual("wip_part", missing[0].Scope);
        Assert.AreEqual("WIP", missing[0].SystemName);
        Assert.AreEqual(PartPictureLayout.CoilFolder, missing[0].FamilyFolder);
    }

    [TestMethod]
    public async Task APartTheStoreHoldsForTheOtherSystem_IsStillMissing()
    {
        // The Visual part is pictured; the WIP part that shares its number is not (FR-002).
        _readService.AddOverride("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");

        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000"));

        var missing = await service.GetMissingPartsAsync();

        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual("MMC0001000", missing[0].PartNumber);
        Assert.AreEqual("wip_part", missing[0].Scope);
    }

    [TestMethod]
    public async Task TheSameNumberTwiceInOneSystem_IsListedOnce()
    {
        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000", "mmc0001000"));

        var missing = await service.GetMissingPartsAsync();

        Assert.AreEqual(
            1,
            missing.Count,
            "Two spellings of one number are one file on this machine, so they are one piece of work.");
    }

    [TestMethod]
    public async Task ASourceThatCannotBeRead_ContributesNothingRatherThanEverything()
    {
        var source = new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000")
        {
            Failure = new InvalidOperationException("the floor is unreachable"),
        };

        var missing = await CreateService(source).GetMissingPartsAsync();

        Assert.AreEqual(
            0,
            missing.Count,
            "An unreadable source is not evidence that its parts have no pictures.");
    }

    [TestMethod]
    public async Task EveryNamedPartPictured_AnswersAnEmptyList()
    {
        _readService.AddOverride("wip_part", "MMC0001000", "WIP/MMC/MMC0001000.png");

        var missing = await CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000")).GetMissingPartsAsync();

        Assert.AreEqual(0, missing.Count);
    }

    [TestMethod]
    public async Task TheExport_CarriesTheHeaderAndOneLinePerPart()
    {
        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000", "FGT0009000"));

        var missing = await service.GetMissingPartsAsync();
        var csv = PartPictureCoverageService.BuildExportCsv(missing);

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(3, lines.Length, "One header and one line per part.");

        StringAssert.Contains(lines[0], "Part number");
        StringAssert.Contains(lines[0], "Family folder");

        Assert.AreEqual("MMC0001000,WIP,MMC,WIP/MMC/MMC0001000", lines[1]);
        Assert.AreEqual("FGT0009000,WIP,FGT,WIP/FGT/FGT0009000", lines[2]);
    }

    [TestMethod]
    public async Task TheExportPath_NamesTheFolderAndTheFileWithoutClaimingAnExtension()
    {
        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "OTHER-1"));

        var missing = await service.GetMissingPartsAsync();

        Assert.AreEqual(
            $"{PartPictureLayout.WipCollection}/{PartPictureLayout.CatchAllFolder}/OTHER-1",
            missing[0].RelativePath,
            "The operator chooses the extension, so the list must not claim one.");
        Assert.AreEqual(PartPictureLayout.CatchAllFolder, missing[0].FamilyFolder);
    }

    [TestMethod]
    public void TheExport_QuotesAValueThatWouldOtherwiseSplitALine()
    {
        var rows = new[]
        {
            new PartPictureMissRow
            {
                Scope = PartPictureLayout.WipPartScope,
                SystemName = "WIP",
                PartNumber = "A,B",
                FamilyFolder = PartPictureLayout.CatchAllFolder,
                RelativePath = "WIP/Categorized Parts/A,B",
            },
        };

        var csv = PartPictureCoverageService.BuildExportCsv(rows);

        StringAssert.Contains(csv, "\"A,B\"");
    }

    [TestMethod]
    public async Task TheSystemsItCanList_AreTheOnesItsSourcesName()
    {
        var service = CreateService(new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000"));

        Assert.AreEqual(1, service.CoveredSystems.Count);
        Assert.AreEqual(PartPictureSystem.Wip, service.CoveredSystems[0]);
    }

    private PartPictureCoverageService CreateService(params IPartNumberSource[] sources) =>
        new(sources, _readService, NullLogger<PartPictureCoverageService>.Instance);
}
