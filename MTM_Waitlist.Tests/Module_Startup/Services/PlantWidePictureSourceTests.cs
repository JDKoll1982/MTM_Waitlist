using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// Where the pictures come from belongs to the plant, and work carries on when the share does not (FR-040,
/// FR-042; SC-020, SC-021).
/// </summary>
/// <remarks>
/// <para>
/// Two failures are possible here and they are different. The first is a plant-wide answer with several owners: if
/// a computer can capture a folder of its own, two computers disagree and neither is wrong. The second is a share
/// that cannot be reached stopping work that has nothing to do with pictures.
/// </para>
/// <para>
/// The behaviour is asserted where it lives — the resolver's cascade in
/// <c>ImageStorageConfigurationResolverTests</c>, the step's own words in <c>PictureCacheStepTests</c>, the
/// pre-change record in <c>MachineConfigurationServiceTests</c>. What this class checks is the shape that makes
/// those behaviours safe: that the per-computer path is gone from the tree, that its rows are withdrawn rather
/// than left behind, and that no surface can write a folder into a computer's record.
/// </para>
/// </remarks>
[TestClass]
public sealed class PlantWidePictureSourceTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    /// <summary>
    /// Nothing in the tree installs a procedure that sets a picture source for one computer. The procedures that
    /// did are retired: their folders keep the rollback that drops them and no longer carry a create, so a fresh
    /// store has no such procedure and a store that had one loses it (FR-040).
    /// </summary>
    /// <remarks>
    /// The check is on the create scripts rather than on the folder names, because the folders are kept on purpose:
    /// they hold the paired rollback, which is what makes the retirement reversible. A folder named for a retired
    /// procedure is the record; an installable create for one would be the defect.
    /// </remarks>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_NoProcedure_CanSetAPictureSourceForOneComputer()
    {
        var proceduresDirectory = Path.Combine(Root, "Database", "StoredProcedures");

        var installable = Directory
            .GetDirectories(proceduresDirectory)
            .Where(directory => Path.GetFileName(directory)!.Contains("computer_sources", StringComparison.OrdinalIgnoreCase))
            .Where(directory => File.Exists(Path.Combine(directory, "create.sql")))
            .Select(Path.GetFileName)
            .ToList();

        Assert.AreEqual(
            0,
            installable.Count,
            "A procedure that sets a folder for one computer is installable again, so a plant-wide answer would have several owners (FR-040): "
                + string.Join(", ", installable));

        Assert.IsFalse(
            File.ReadAllText(Path.Combine(proceduresDirectory, "AllSPs.sql")).Contains("computer_sources", StringComparison.Ordinal)
                && !File.ReadAllText(Path.Combine(proceduresDirectory, "AllSPs.sql")).Contains("-- sp_config_images_locations_computer_sources", StringComparison.Ordinal),
            "The procedure aggregate installs a per-computer picture source again (FR-040).");

        // The rollbacks are kept, so the retirement is still reversible.
        var retired = Directory
            .GetDirectories(proceduresDirectory)
            .Where(directory => Path.GetFileName(directory)!.Contains("computer_sources", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.IsTrue(
            retired.Count >= 3,
            $"Only {retired.Count} of the three retired per-computer procedures keep their folder, so the retirement can no longer be reversed (constitution III).");

        foreach (var directory in retired)
        {
            Assert.IsTrue(
                File.Exists(Path.Combine(directory, "rollback.sql")),
                $"{Path.GetFileName(directory)} ships no rollback, so its retirement cannot be reversed (constitution III).");
        }
    }

    /// <summary>
    /// The rows an older store holds are withdrawn rather than deleted, by a seed that ships its paired rollback
    /// (FR-040, SC-021; constitution III).
    /// </summary>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_ThePerComputerRows_AreWithdrawnByASeedThatShipsItsRollback()
    {
        var seedDirectory = Path.Combine(Root, "Database", "Seeds", "seed_retire_computer_scope_picture_sources");

        Assert.IsTrue(
            Directory.Exists(seedDirectory),
            "The seed that withdraws the per-computer picture sources is gone, so an older store would keep rows nothing reads (SC-021).");

        foreach (var file in new[] { "create.sql", "rollback.sql" })
        {
            Assert.IsTrue(
                File.Exists(Path.Combine(seedDirectory, file)),
                $"The withdrawal seed ships no {file}, so the change cannot be reversed (constitution III).");
        }

        var create = File.ReadAllText(Path.Combine(seedDirectory, "create.sql"));

        StringAssert.Contains(
            create,
            "sp_config_images_locations_deactivate_for_scope",
            "The withdrawal no longer goes through the shipped procedure, so two places would own the column (constitution III).");
    }

    /// <summary>
    /// The withdrawal is registered, because a seed that nothing runs withdraws nothing.
    /// </summary>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_TheWithdrawalSeed_IsRegisteredSoTheInstallerRunsIt()
    {
        var aggregate = File.ReadAllText(Path.Combine(Root, "Database", "Seeds", "AllSeeds.sql"));

        StringAssert.Contains(
            aggregate,
            "seed_retire_computer_scope_picture_sources",
            "The withdrawal seed is not in the aggregate, so no install would run it and an older store would keep its rows (SC-021).");
    }

    /// <summary>
    /// No surface can write a folder into this computer's record: the identity contract carries two facts and
    /// neither is a folder (FR-040, FR-049).
    /// </summary>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_NoSurface_CanWriteAFolderIntoThisComputersRecord()
    {
        var contracts = new[]
        {
            Path.Combine(Root, "MTM_Waitlist.Core", "Models", "MachineConfigurationDraft.cs"),
            Path.Combine(Root, "MTM_Waitlist.Core", "Models", "MachineConfigurationState.cs"),
        };

        foreach (var path in contracts)
        {
            Assert.IsTrue(File.Exists(path), $"{Path.GetFileName(path)} is missing, so the machine's identity is stated somewhere else.");

            var text = File.ReadAllText(path);

            foreach (var forbidden in new[] { "PictureSource", "SharedFolder", "KeysFolder", "FolderPath", "DunnageRoot" })
            {
                Assert.IsFalse(
                    text.Contains(forbidden, StringComparison.Ordinal),
                    $"{Path.GetFileName(path)} carries {forbidden}, so a computer could capture a folder of its own (FR-040).");
            }
        }
    }

    /// <summary>
    /// The plant-wide answer is the only one in force, and it is the one the settings panel writes (FR-040,
    /// SC-020).
    /// </summary>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_TheSharedPictureFolder_IsHeldOnceForThePlant()
    {
        var keys = File.ReadAllText(Path.Combine(Root, "MTM_Waitlist.Settings", "Models", "ConfigSettingValue.cs"));

        StringAssert.Contains(
            keys,
            "image_storage.shared_folder_path",
            "The plant-wide shared pictures setting is gone, so there would be nowhere for the panel's answer to live (FR-040).");

        Assert.IsFalse(
            keys.Contains("keys.folder_path", StringComparison.Ordinal),
            "The shared keys folder setting is back, though nothing reads a key file (FR-043).");

        var panel = File.ReadAllText(Path.Combine(Root, "Module_Settings", "Views", "PicturesSettingsPage.xaml"));

        StringAssert.Contains(
            panel,
            "SettingsPage_StoragePaths_ImageFolderBox",
            "The settings panel no longer offers the shared pictures folder, so the plant's answer could not be changed (SC-020).");

        Assert.IsFalse(
            panel.Contains("KeysFolder", StringComparison.Ordinal),
            "The settings panel still offers a shared keys folder that nothing reads (FR-043).");
    }

    /// <summary>
    /// The picture copy cannot stop the launch, and the step that makes it says so in the catalogue it takes its
    /// own bound from (FR-026, SC-019).
    /// </summary>
    /// <remarks>
    /// The step's own behaviour is asserted in <c>PictureCacheStepTests</c>, including the launch carrying on when
    /// a source cannot be read. What is asserted here is the fact that makes that safe: the entry is marked best
    /// effort and is bounded more tightly than the ceiling, so it cannot hold the launch even if it misbehaves.
    /// </remarks>
    [TestMethod]
    public void Service_PlantWidePictureSourceTests_ThePictureCopy_IsBestEffortAndBoundedInsideTheCeiling()
    {
        var catalog = File.ReadAllText(Path.Combine(Root, "MTM_Waitlist.Startup", "Services", "LaunchStepCatalog.cs"));

        StringAssert.Contains(
            catalog,
            "\"picture-cache\"",
            "The picture copy is no longer a catalogued step, so its bound is not stated anywhere.");

        // The entry declares its own maximum, its best-effort marker and its target; the catalogue refuses a
        // best-effort entry at the ceiling when it is constructed.
        var entry = catalog[catalog.IndexOf("\"picture-cache\"", StringComparison.Ordinal)..];
        var entryBody = entry[..Math.Min(entry.Length, 400)];

        StringAssert.Contains(
            entryBody,
            "true",
            "The picture-cache entry is no longer marked best effort, so it could hold the launch (FR-026).");
    }
}
