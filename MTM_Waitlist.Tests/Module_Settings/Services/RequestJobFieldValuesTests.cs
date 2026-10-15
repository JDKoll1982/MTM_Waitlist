using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// A declared detail row whose source is <c>job</c> names a value the requesting job carries and the request never
/// stored, so the request page could not draw it until the job was handed in (FR-052, FR-053).
/// </summary>
[TestClass]
public sealed class RequestJobFieldValuesTests
{
    private static RequestJobPartAvailability DieJob() => RequestJobPartAvailability.None with
    {
        HasActiveJob = true,
        HasDie = true,
        JobPartNumber = "PART-9003",
        DieNumber = "FGT0002000",
        DieLocation = "DIE SHOP",
    };

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_PartTypeAndItemTypeReadTheSnapshotsDerivation()
    {
        // The four returning pickups declare `Part Type` and the component Items declare `Item Type`, and both
        // read the one derivation the snapshot was handed (FR-091, FR-092, research R1). Neither label runs a
        // derivation of its own.
        var finishedGoods = RequestJobPartAvailability.None with { Disposition = RequestDisposition.FinishedGoods };
        var outsideService = RequestJobPartAvailability.None with { Disposition = RequestDisposition.OutsideService };
        var wip = RequestJobPartAvailability.None with { Disposition = RequestDisposition.WorkInProcess };
        var ncm = RequestJobPartAvailability.None with { Disposition = RequestDisposition.Ncm };

        Assert.AreEqual("Finished Goods", RequestJobFieldValues.Resolve("Part Type", finishedGoods));
        Assert.AreEqual("Finished Goods", RequestJobFieldValues.Resolve("Item Type", finishedGoods));
        Assert.AreEqual("Outside Service", RequestJobFieldValues.Resolve("Part Type", outsideService));
        Assert.AreEqual("WIP", RequestJobFieldValues.Resolve("Part Type", wip));
        Assert.AreEqual("NCM", RequestJobFieldValues.Resolve("Part Type", ncm));
        Assert.AreEqual("NCM", RequestJobFieldValues.Resolve("Item Type", ncm));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_AnUnderivedItemTypeResolvesNothingSoTheCellIsLeftOut()
    {
        // Nothing has been derived for this job, so the cell resolves nothing and is not drawn — never a blank
        // standing in for a value and never an invented one (FR-089).
        Assert.IsNull(RequestJobFieldValues.Resolve("Part Type", RequestJobPartAvailability.None));
        Assert.IsNull(RequestJobFieldValues.Resolve("Item Type", RequestJobPartAvailability.All));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_ItemTypeIsNeverReadFromAPartsRecordedItemType()
    {
        // FR-093: the value comes from the derivation the snapshot carries, so a snapshot that carries no
        // derivation answers nothing whatever else it holds.
        var jobWithEverythingButADerivation = RequestJobPartAvailability.All with
        {
            ScrapType = "Steel Offal",
            CoilPartNumber = "COIL-1",
        };

        Assert.IsNull(RequestJobFieldValues.Resolve("Part Type", jobWithEverythingButADerivation));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_DieIsTheNumberAndTheLocationTogether()
    {
        Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve("Die", DieJob()));
    }

    /// <summary>A job carrying two dies, with the second one the request is actually for (FR-057).</summary>
    private static RequestJobPartAvailability TwoDieJob() => DieJob().WithDies(
    [
        new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" },
        new RequestDiePart { PartNumber = "D-9001", Location = "PRESS BAY" },
    ]);

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_DieListsEveryDieTheJobCarriesNotJustTheFirst()
    {
        // FR-057: a job carrying more than one die is one row per die, never the first die standing in for all
        // of them. The requester reads the row to see which dies the job has.
        Assert.AreEqual(
            "FGT0002000, D-9001",
            RequestJobFieldValues.Resolve("Die", TwoDieJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_PickupLocationListsEveryDieLocationTheJobCarries()
    {
        // FR-057, stated directly: the request page lists every die location. The locations are drawn in the
        // job's own order, so the row reads in the same order as the job does.
        Assert.AreEqual(
            "DIE SHOP, PRESS BAY",
            RequestJobFieldValues.Resolve("Pickup location", TwoDieJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_DieWithADieThatHasNoLocationLeavesNoDanglingSeparatorInTheList()
    {
        // The second die has no recorded location, so its entry is its number alone and no separator is left
        // hanging off it.
        var twoDies = DieJob().WithDies(
        [
            new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" },
            new RequestDiePart { PartNumber = "D-9001", Location = string.Empty },
        ]);

        Assert.AreEqual("FGT0002000, D-9001", RequestJobFieldValues.Resolve("Die", twoDies));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_PickupLocationWithOnlyOneDieThatHasNoLocationYieldsNothing()
    {
        // A location the job does not record is omitted rather than drawn as a blank standing in for a value.
        var noLocations = DieJob().WithDies(
        [
            new RequestDiePart { PartNumber = "FGT0002000", Location = string.Empty },
            new RequestDiePart { PartNumber = "D-9001", Location = string.Empty },
        ]);

        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noLocations));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_PickupLocationIsWhereTheDieIs()
    {
        Assert.AreEqual("DIE SHOP", RequestJobFieldValues.Resolve("Pickup location", DieJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_DieLocationIsTheCorrectedLabelForTheSameValue()
    {
        // FR-059 moves the die Items' seed label from `Pickup location` to `Die Location`. Both spellings must
        // answer the same value, or the seed move blanks the die cell.
        Assert.AreEqual(
            RequestJobFieldValues.Resolve("Pickup location", DieJob()),
            RequestJobFieldValues.Resolve("Die Location", DieJob()));
        Assert.AreEqual("DIE SHOP", RequestJobFieldValues.Resolve("Die Location", DieJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_PartNumberIsThePartTheDieIsAssignedTo()
    {
        Assert.AreEqual("PART-9003", RequestJobFieldValues.Resolve("Part number", DieJob()));
    }

    /// <summary>The job's material the resolver's new arms read — its coil, its flatstock and its scrap decision.</summary>
    private static RequestJobPartAvailability MaterialJob() => DieJob() with
    {
        CoilPartNumber = "MMC0001000",
        FlatstockPartNumber = "MMF0000500",
        ScrapType = "HEAVY MELT",
        CoilDescription = "0.250 X 36.000 COIL",
        CoilAverageWeight = "4820",
    };

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CoilNumberIsTheCoilTheRequestingJobCarries()
    {
        // FR-088: the snapshot already holds this value and the resolver returned nothing for either spelling.
        foreach (var label in new[] { "Coil number", "Coil Number" })
        {
            Assert.AreEqual("MMC0001000", RequestJobFieldValues.Resolve(label, MaterialJob()));
        }
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CorrectCoilIsTheJobsOwnCoil()
    {
        // The wrong-coil Item's correct coil is the coil the job carries — never the incorrect coil typed on the
        // card, which arrives as the request's own answer (FR-082).
        Assert.AreEqual("MMC0001000", RequestJobFieldValues.Resolve("Correct coil", MaterialJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CorrectFlatstockIsTheFlatstockTheJobCarries()
    {
        Assert.AreEqual("MMF0000500", RequestJobFieldValues.Resolve("Correct flatstock", MaterialJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_ScrapTypeIsTheDecisionTheJobAlreadyRecorded()
    {
        Assert.AreEqual("HEAVY MELT", RequestJobFieldValues.Resolve("Scrap Type", MaterialJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CoilDescriptionIsTheDescriptionTheCoilsPartRecords()
    {
        Assert.AreEqual("0.250 X 36.000 COIL", RequestJobFieldValues.Resolve("Coil description", MaterialJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_AverageCoilWeightIsTheMaterialsAverageNotAWeighedFigure()
    {
        // FR-054, FR-086: this is the plant's average for the coil's part, so the assist coil turn's own weighed
        // `Weight of Coil` answer never comes from here.
        Assert.AreEqual("4820", RequestJobFieldValues.Resolve("Average coil weight", MaterialJob()));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_HomeLocationListsEveryLocationTheJobsDunnagePartsRecord()
    {
        var dunnage = MaterialJob().WithDunnageParts(
        [
            new RequestDunnagePart { PartNumber = "D-100", HomeLocation = "AISLE 4" },
            new RequestDunnagePart { PartNumber = "D-200", HomeLocation = "RACK B" },
        ]);

        Assert.AreEqual("AISLE 4, RACK B", RequestJobFieldValues.Resolve("Home Location", dunnage));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_HomeLocationWithNothingRecordedYieldsNothing()
    {
        // A location the job does not record is omitted rather than invented or drawn blank (FR-058, FR-089).
        var noLocations = MaterialJob().WithDunnageParts(
        [
            new RequestDunnagePart { PartNumber = "D-100", HomeLocation = "  " },
            new RequestDunnagePart { PartNumber = "D-200" },
        ]);

        Assert.IsNull(RequestJobFieldValues.Resolve("Home Location", noLocations));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_BlankSnapshotFieldsYieldNothing()
    {
        // The caller omits the cell rather than drawing it empty (FR-058, FR-089).
        var blank = MaterialJob() with
        {
            CoilPartNumber = " ",
            FlatstockPartNumber = string.Empty,
            ScrapType = string.Empty,
            CoilDescription = string.Empty,
            CoilAverageWeight = "  ",
        };

        Assert.IsNull(RequestJobFieldValues.Resolve("Coil number", blank));
        Assert.IsNull(RequestJobFieldValues.Resolve("Correct coil", blank));
        Assert.IsNull(RequestJobFieldValues.Resolve("Correct flatstock", blank));
        Assert.IsNull(RequestJobFieldValues.Resolve("Scrap Type", blank));
        Assert.IsNull(RequestJobFieldValues.Resolve("Coil description", blank));
        Assert.IsNull(RequestJobFieldValues.Resolve("Average coil weight", blank));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CapturedAnswerWinsOverTheJobArmForItsOwnLabel()
    {
        // FR-082, and the wrong-part Items' incorrect part: what the requester captured is the request's own
        // value, so the job does not answer over the top of it.
        var answers = new Dictionary<string, string> { ["Correct flatstock"] = "MMF0000999" };

        Assert.AreEqual("MMF0000999", RequestJobFieldValues.Resolve("Correct flatstock", MaterialJob(), answers));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_CapturedAnswerIsMatchedByTheSameTrimmedCaseInsensitiveLabel()
    {
        // The label comparison is the same one the arms use, so an answer's own spacing cannot decide the row.
        var answers = new Dictionary<string, string> { ["  CORRECT FLATSTOCK  "] = "MMF0000999" };

        Assert.AreEqual("MMF0000999", RequestJobFieldValues.Resolve("Correct Flatstock", MaterialJob(), answers));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_AnswerUnderAnotherLabelDoesNotAnswerForTheDeclaredOne()
    {
        // `Coil number` is answer-sourced on one Item and job-sourced elsewhere, so the declared label — not the
        // set of answers — decides which value the row reads.
        var answers = new Dictionary<string, string> { ["Coil number"] = "MMC0009999" };

        Assert.AreEqual("MMC0001000", RequestJobFieldValues.Resolve("Correct coil", MaterialJob(), answers));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_BlankCapturedAnswerFallsThroughToTheJobArm()
    {
        // A blank is not an answer (FR-006), so the job still supplies the value rather than the row being drawn
        // from nothing.
        var answers = new Dictionary<string, string> { ["Correct flatstock"] = "   " };

        Assert.AreEqual("MMF0000500", RequestJobFieldValues.Resolve("Correct flatstock", MaterialJob(), answers));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_WithoutCapturedAnswersTheJobArmStillAnswers()
    {
        Assert.AreEqual("MMC0001000", RequestJobFieldValues.Resolve("Coil number", MaterialJob(), answers: null));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_IsCaseAndSpacingInsensitive()
    {
        // The label comes from the Item's stored configuration, so the row's own spacing must not decide it.
        foreach (var label in new[] { "die", "DIE", "  Die  " })
        {
            Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve(label, DieJob()));
        }
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_DieWithNoKnownLocationLeavesNoDanglingSeparator()
    {
        var noLocation = DieJob() with { DieLocation = string.Empty };

        Assert.AreEqual("FGT0002000", RequestJobFieldValues.Resolve("Die", noLocation));
        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noLocation), "a location it does not have is not drawn.");
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_JobWithNoDieYieldsNothingSoNoEmptyRowIsDrawn()
    {
        var noDie = RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = false };

        Assert.IsNull(RequestJobFieldValues.Resolve("Die", noDie));
        Assert.IsNull(RequestJobFieldValues.Resolve("Pickup location", noDie));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_WithoutAJobYieldsNothing()
    {
        // The page resolves before the job arrives; it must show the rows the request itself carries and no more.
        Assert.IsNull(RequestJobFieldValues.Resolve("Die", job: null));
    }

    [TestMethod]
    public void Service_RequestJobFieldValuesTests_Resolve_UnknownOrEmptyLabelYieldsNothing()
    {
        // A label this snapshot cannot supply is omitted rather than drawn as a blank standing in for a value.
        // `Lugger Location` is declared but has no source anywhere, so it is absent by design (research R1).
        Assert.IsNull(RequestJobFieldValues.Resolve("Lugger Location", DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve("Quantity", DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve(string.Empty, DieJob()));
        Assert.IsNull(RequestJobFieldValues.Resolve(null, DieJob()));
    }
}
