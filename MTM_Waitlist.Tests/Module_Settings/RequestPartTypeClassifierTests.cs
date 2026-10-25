using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The part type is read from the plant's own Infor Visual codes, with the part number's prefix and the description
/// deciding only what kind of customer-supplied material a part is. The cases below are real rows from the live
/// install (VISUAL/MTMFG, 2026-10-23), including the two families that would be misfiled by description alone.
/// </summary>
[TestClass]
public sealed class RequestPartTypeClassifierTests
{
    [TestMethod]
    [DataRow("RM_PRESS", "M Matl Coil", "1100259", "Coil, .018 X 24.203", RequestPartType.Coil)]
    [DataRow("RM_PRESS", "S Matl Coil", "MMC0000045", "Coil, 10Ga X 7.500", RequestPartType.Coil)]
    [DataRow("RM_PRESS", "M Matl Flat", "0006 1011", "Blank, 20Ga X 14.500 X 14.500", RequestPartType.Flatstock)]
    [DataRow("RM_PRESS", "M Fast Fastener", "08914 26800", "Nut, Weld Square M8-1.25", RequestPartType.Hardware)]
    [DataRow("RM_PRESS", "M Matl Round", "1611976", "Pin - Weld Projection", RequestPartType.BarRound)]
    [DataRow("RM_PRESS", "M Matl Net-Shap", "04-27802-000-unp", "Brkt-Stanchion Supt, Vert ATS", RequestPartType.RawOther)]
    [DataRow("FG_PRESS", "E Stamped Item", "000002967", "Channel, RH Corner", RequestPartType.StampedPart)]
    [DataRow("FG_PRESS", "E Assy Item", "19 126 20", "Bracket, Assembly Throttle Control", RequestPartType.Assembly)]
    [DataRow("FG_PRESS", "E Mach Item", "01-33371-000", "Adapter-PTO, 1350 to 1310", RequestPartType.MachinedPart)]
    [DataRow("MFG_TOOLING", "T MFG Use Tools", "27442", "Progressive Complete", RequestPartType.Tooling)]
    [DataRow("FG_DIE", "M Die Punch", ".312 X .188", "Punch, CPM", RequestPartType.DieComponent)]
    [DataRow("FG_DIE", "E Tooling", ".054\" X .500\"", "Punch, CPM", RequestPartType.DieComponent)]
    [DataRow("EXP_PKG", "", "075-F-00321", "Bag, 15\"x9\"x24\" Zerust-2 Mil", RequestPartType.Expense)]
    public void Service_RequestPartTypeClassifierTests_Classify_ReadsTheCommodityCode(
        string productCode,
        string commodityCode,
        string partNumber,
        string description,
        RequestPartType expected)
    {
        Assert.AreEqual(
            expected,
            RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(productCode, commodityCode, partNumber, description)));
    }

    /// <summary>
    /// A part named after the fastener it retains is a stamped part, not hardware: the description mentions the word
    /// but the plant's own code says what the part is. 50 live parts look like this.
    /// </summary>
    [TestMethod]
    public void Service_RequestPartTypeClassifierTests_Classify_AStampedPartNamedAfterABoltIsNotHardware()
    {
        var classification = RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(
            "FG_PRESS", "E Stamped Item", "01-34886-000", "Brkt, Bolt Rtner, 43N, Lower"));

        Assert.AreEqual(RequestPartType.StampedPart, classification);
    }

    /// <summary>
    /// Hardware whose description never says so is still hardware — 73 live parts are shaped like this.
    /// </summary>
    [TestMethod]
    public void Service_RequestPartTypeClassifierTests_Classify_HardwareTheDescriptionDoesNotName()
    {
        var classification = RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(
            "RM_PRESS", "M Fast Fastener", "22-823465", "Plug, Pipe, 1.000-11.500 NPT"));

        Assert.AreEqual(RequestPartType.Hardware, classification);
    }

    /// <summary>
    /// Customer-supplied material is the one family whose commodity code does not say what it is, so the part
    /// number's own prefix decides first — the plant's <c>MMC</c> / <c>MMF</c> / <c>MMR</c> families, with the
    /// <c>CS</c> and <c>SR</c> suffixes riding on the same prefix.
    /// </summary>
    [TestMethod]
    [DataRow("MMCCS0000123", "", RequestPartType.Coil)]
    [DataRow("MMCSR0000123", "", RequestPartType.Coil)]
    [DataRow("MMFCS0000045", "", RequestPartType.Flatstock)]
    [DataRow("MMFSR0000045", "", RequestPartType.Flatstock)]
    [DataRow("MMR0000018", "", RequestPartType.BarRound)]
    [DataRow("09887633", "Coil, .250 X 19.750", RequestPartType.Coil)]
    [DataRow("101451-06", "Coil, 20Ga  X 14.500", RequestPartType.Coil)]
    [DataRow("55667788", "Sheet, 18Ga X 24.000 X 48.000", RequestPartType.Flatstock)]
    [DataRow("99887766", "Something Else", RequestPartType.CustomerSupplied)]
    public void Service_RequestPartTypeClassifierTests_Classify_CustomerSuppliedMaterial(
        string partNumber,
        string description,
        RequestPartType expected)
    {
        Assert.AreEqual(
            expected,
            RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(
                "RM_PRESS", "M Matl Cust Sup", partNumber, description)));
    }

    [TestMethod]
    public void Service_RequestPartTypeClassifierTests_Classify_NothingNamedIsUnknown()
    {
        Assert.AreEqual(
            RequestPartType.Unknown,
            RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(null, null, null, null)));

        Assert.AreEqual(
            RequestPartType.Unknown,
            RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput("FG_PRESS", "", "12345", "Brkt-Pass Thru")));
    }

    [TestMethod]
    public void Service_RequestPartTypeClassifierTests_Classify_CodeMatchingIgnoresCaseAndSurroundingSpace()
    {
        var classification = RequestPartTypeClassifier.Classify(new RequestPartTypeClassifier.PartTypeInput(
            " rm_press ", "  m matl coil ", "1100259", "Coil, .018 X 24.203"));

        Assert.AreEqual(RequestPartType.Coil, classification);
    }

    [TestMethod]
    public void Model_RequestPartTypeClassifierTests_Labels_EveryNamedKindHasAWordAndUnknownHasNone()
    {
        foreach (var value in Enum.GetValues<RequestPartType>())
        {
            var label = RequestPartTypeLabels.Of(value);

            if (value == RequestPartType.Unknown)
            {
                Assert.IsNull(label, "Nothing named a kind, so the cell is left out rather than guessed (FR-089).");
                continue;
            }

            Assert.IsFalse(string.IsNullOrWhiteSpace(label), $"{value} must have a word a card can draw.");
        }
    }
}
