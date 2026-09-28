using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The cause-to-remedy table (`contracts/launch-step-contract.md` §4,
/// `contracts/machine-configuration-contract.md` §4; FR-016, FR-017, FR-018, FR-019, SC-010): every stopping
/// condition offers only actions that could remove its cause, a store that cannot be reached never offers a
/// reset, and the reset that is offered names exactly what it will touch.
/// </summary>
/// <remarks>
/// The table is asserted directly rather than through the surface, because it is the artifact the requirements
/// are stated against. A surface that drew the right controls from a wrong table would be right by accident.
/// </remarks>
[TestClass]
public sealed class BlockedStateRemediesTests
{
    private const string ResourceFile = @"Strings/en-us/Resources.resw";

    [TestMethod]
    public void Classify_WhenTheStoreCouldNotBeReached_IsTheStoreCause()
    {
        var catalog = new LaunchStepCatalog();

        var cause = BlockedStateRemedies.Classify(catalog.Find("store-reachability"));

        Assert.AreEqual(BlockedStateCause.StoreUnreachable, cause);
    }

    [TestMethod]
    public void Classify_WhenThisComputersConfigurationCouldNotBeWritten_IsTheConfigurationCause()
    {
        var catalog = new LaunchStepCatalog();

        var cause = BlockedStateRemedies.Classify(catalog.Find("save-machine-configuration"));

        Assert.AreEqual(BlockedStateCause.MachineConfigurationBroken, cause);
    }

    [TestMethod]
    public void Classify_WhenThisComputersConfigurationCouldNotBeRead_IsTheConfigurationCause()
    {
        var catalog = new LaunchStepCatalog();

        var cause = BlockedStateRemedies.Classify(catalog.Find("read-machine-configuration"));

        Assert.AreEqual(BlockedStateCause.MachineConfigurationBroken, cause);
    }

    [TestMethod]
    public void Classify_WhenTheFeedNamedNoFailure_IsTheStoreCause()
    {
        // Nothing named the fault, so the answer is the one offering the least: a repeat and an ending, and no
        // reset that could throw away a configuration for a fault that was never about it (FR-017).
        var cause = BlockedStateRemedies.Classify(null);

        Assert.AreEqual(BlockedStateCause.StoreUnreachable, cause);
        Assert.IsFalse(BlockedStateRemedies.For(cause).CanRestoreDefaults);
    }

    [TestMethod]
    public void For_EveryCause_OffersARepeat()
    {
        // FR-016: a stopped launch offers to repeat the failed work, whatever stopped it.
        foreach (var cause in Enum.GetValues<BlockedStateCause>())
        {
            Assert.IsTrue(
                BlockedStateRemedies.For(cause).CanRetry,
                $"{cause} must offer a repeat, because every failed step is repeatable (FR-016)");
        }
    }

    [TestMethod]
    public void For_WhenTheStoreCouldNotBeReached_OffersNoResetAndNoPreview()
    {
        // FR-017, SC-010, US4 scenario 1: the store cannot be reached, so nothing this computer's configuration
        // can be restored to could remove the cause and a reset is not offered at all.
        var remedies = BlockedStateRemedies.For(BlockedStateCause.StoreUnreachable);

        Assert.IsFalse(remedies.CanRestoreDefaults, "a store outage must never offer a reset");
        Assert.IsNull(remedies.RestoreDefaultsPreview, "a reset that is not offered has nothing to name");
        Assert.AreEqual(0, BlockedStateRemedies.ResetPartsFor(BlockedStateCause.StoreUnreachable).Count);
    }

    [TestMethod]
    public void For_WhenThisComputersConfigurationIsBroken_OffersAResetThatNamesEveryPartItWouldTouch()
    {
        // FR-018: the preview names exactly what will be reset before anything is reset. The sentence is composed
        // from the parts themselves, so it cannot name a set the reset does not restore.
        var remedies = BlockedStateRemedies.For(BlockedStateCause.MachineConfigurationBroken);

        Assert.IsTrue(remedies.CanRestoreDefaults, "a broken configuration is what a reset could repair (FR-017)");
        Assert.IsFalse(string.IsNullOrWhiteSpace(remedies.RestoreDefaultsPreview), "the preview must say something");

        foreach (var part in BlockedStateRemedies.ResetPartsFor(BlockedStateCause.MachineConfigurationBroken))
        {
            StringAssert.Contains(
                remedies.RestoreDefaultsPreview!,
                BlockedStateRemedies.DescribePart(part),
                $"the preview must name '{part}', because the reset will touch it (FR-018)");
        }
    }

    [TestMethod]
    public void ResetParts_EveryCause_NamesOnlyThisComputersOwnConfiguration()
    {
        // FR-018: a reset affects only this machine's configuration and never a person, a role or a permission.
        // Every token the table can produce is therefore one of the configuration parts' own.
        var thisMachinesOwn = new[]
        {
            MachineConfigurationParts.Configuration,
            MachineConfigurationParts.DisplayName,
            MachineConfigurationParts.Description,
            MachineConfigurationParts.PictureSources,
            MachineConfigurationParts.ScopedPreference,
        };

        foreach (var cause in Enum.GetValues<BlockedStateCause>())
        {
            foreach (var part in BlockedStateRemedies.ResetPartsFor(cause))
            {
                CollectionAssert.Contains(
                    thisMachinesOwn,
                    part,
                    $"'{part}' is not this computer's own configuration, so a reset must never be asked for it");
            }
        }
    }

    [TestMethod]
    public void SilentRepairParts_AreThePartsTheScopeSupplies_AndNeverAChoiceThePersonMade()
    {
        // FR-019: a fault put right without asking may not throw away anything the person decided. The display
        // name, the description and the picture sources are their decisions, so none of them may be repaired
        // quietly; the one part the scope supplies is what may be.
        var silent = BlockedStateRemedies.SilentRepairPartsFor(BlockedStateCause.MachineConfigurationBroken);

        CollectionAssert.AreEqual(
            new[] { MachineConfigurationParts.ScopedPreference },
            silent.ToArray(),
            "only the part whose value comes from the scope may be repaired without asking (FR-019)");

        CollectionAssert.DoesNotContain(silent.ToArray(), MachineConfigurationParts.DisplayName);
        CollectionAssert.DoesNotContain(silent.ToArray(), MachineConfigurationParts.Description);
        CollectionAssert.DoesNotContain(silent.ToArray(), MachineConfigurationParts.PictureSources);
    }

    [TestMethod]
    public void SilentRepairParts_WhenTheStoreCouldNotBeReached_AreNone()
    {
        // A store that cannot be reached has nothing this computer can repair on its own, so the person is asked
        // rather than a repair being attempted that could not succeed (FR-017, FR-019).
        Assert.AreEqual(0, BlockedStateRemedies.SilentRepairPartsFor(BlockedStateCause.StoreUnreachable).Count);
    }

    [TestMethod]
    public void SilentRepairParts_AndTheRecoveryServicesOwnList_AreTheSameSet()
    {
        // The table says which cause has a repair to make quietly and the recovery service says which parts such a
        // repair may touch. The two views of one rule are pinned together here, so neither can drift into
        // repairing something the other would refuse (FR-019).
        var table = BlockedStateRemedies.SilentRepairPartsFor(BlockedStateCause.MachineConfigurationBroken);

        CollectionAssert.AreEqual(
            StartupRecoveryService.PartsRepairableWithoutAsking.ToArray(),
            table.ToArray(),
            "the table and the recovery service must agree on what may be repaired without asking");
    }

    [TestMethod]
    public void BlockedStateWording_ShipsAsLocalisedStrings()
    {
        // The suite runs outside the application, so a resource lookup answers the key itself. The wording the
        // surface and the table draw is therefore checked where it lives, which is the resource file (S15: an
        // unlocalised stop reads as instability).
        var resources = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ResourceFile));
        var keys = new[]
        {
            "Startup_BlockedState.Heading",
            "Startup_BlockedState.Subtitle",
            "Startup_BlockedState.DiagnosisLabel",
            "Startup_BlockedState.RetryAction",
            "Startup_BlockedState.RestoreDefaultsAction",
            "Startup_BlockedState.CloseAction",
            "Startup_BlockedState.CloseReason",
            "Startup_BlockedState.ResetPreviewTitle",
            "Startup_BlockedState.ResetPreviewConfirm",
            "Startup_BlockedState.ResetPreviewCancel",
            "Startup_BlockedState.ResetRefused",
            "Startup_BlockedState.ResetFailed",
            "Startup_BlockedState.PartDisplayName",
            "Startup_BlockedState.PartDescription",
            "Startup_BlockedState.PartPictureSources",
            "Startup_BlockedState.PartScopedPreference",
        };

        foreach (var key in keys)
        {
            StringAssert.Contains(resources, $"name=\"{key}\"", $"'{key}' has to ship as a localised string");
        }
    }

    [TestMethod]
    public void DescribePart_WhenTheTokenNamesNothingKnown_SaysTheTokenItself()
    {
        // A preview that quietly dropped something would be a preview that did not name what the reset would
        // touch, which is the whole of FR-018.
        Assert.AreEqual("something_else", BlockedStateRemedies.DescribePart(" something_else "));
    }
}
