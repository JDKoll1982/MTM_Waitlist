using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The archive cleanup the startup cache step runs: a picture a replace archived is removed once the configured
/// retention period has passed, and nothing else is (US7, FR-011, FR-037, SC-006).
/// </summary>
/// <remarks>
/// <para>
/// The cleanup is part of the cache step rather than a step of its own, because it walks the picture roots the
/// mirror already walks. These cases therefore drive the step exactly as startup drives it —
/// <see cref="ImageCacheSyncService.SynchronizeAsync"/> over the picture root — so what is pinned is the rule the
/// startup step applies, not a helper standing beside it.
/// </para>
/// <para>
/// The archives are named by <c>ImageStorageService</c>, which copies the picture it replaces into an
/// <c>Archive</c> folder beside it as <c>{name}-MM-dd-yyyy-NN{extension}</c>. Nothing here writes that naming rule
/// down a second time: the cases write the names the storage service would write.
/// </para>
/// </remarks>
[TestClass]
public sealed class StartupArchiveCleanupTests
{
    private const string CollectionFolder = PartPictureLayout.VisualCollection;
    private const int KeepDays = 30;

    private string _workingDirectory = string.Empty;
    private string _pictureRoot = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = ImageHeaderFixtures.CreateTemporaryDirectory();
        _pictureRoot = Path.Combine(_workingDirectory, "images");
        Directory.CreateDirectory(_pictureRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    /// <summary>Writes one archived picture, archived on the given day, and answers its path.</summary>
    private string WriteArchivedPicture(DateTime archivedOn, int sequence = 1, string baseName = "MMC-12345")
    {
        var archiveFolder = Path.Combine(_pictureRoot, CollectionFolder, "MMC", AppStoragePaths.ArchiveFolderName);
        Directory.CreateDirectory(archiveFolder);

        var path = Path.Combine(
            archiveFolder,
            $"{baseName}-{archivedOn:MM-dd-yyyy}-{sequence:00}.png");

        ImageHeaderFixtures.WritePng(path, 64, 64);
        return path;
    }

    /// <summary>The step exactly as startup runs it, over the picture root and with the store's period.</summary>
    private ImageCacheSyncResult RunCacheStep(int archiveKeepDays = KeepDays) =>
        ImageCacheSyncService.Synchronize(
            [
                new ImageCacheSource(
                    "waitlist pictures",
                    _pictureRoot,
                    Path.Combine(_workingDirectory, "cache"),
                    PartPictureLayout.PartCollectionFolders,
                    archiveKeepDays),
            ]);

    [TestMethod]
    public void AnArchivePastItsPeriodIsRemovedByTheCacheStep()
    {
        var expired = WriteArchivedPicture(DateTime.Now.AddDays(-(KeepDays + 1)));

        var result = RunCacheStep();

        Assert.AreEqual(1, result.ArchivesRemoved, "The startup step says what it cleaned up rather than doing it silently.");
        Assert.IsFalse(File.Exists(expired), "A replaced picture is kept until its period has passed, and then it is not.");
    }

    [TestMethod]
    public void AnArchiveInsideItsPeriodIsKept()
    {
        var fresh = WriteArchivedPicture(DateTime.Now.AddDays(-1));

        var result = RunCacheStep();

        Assert.AreEqual(0, result.ArchivesRemoved);
        Assert.IsTrue(File.Exists(fresh), "A picture replaced yesterday is still there for the person who wants it back.");
    }

    [TestMethod]
    public void AnArchiveOnTheLastDayOfItsPeriodIsKept()
    {
        var onTheBoundary = WriteArchivedPicture(DateTime.Now.AddDays(-KeepDays));

        var result = RunCacheStep();

        Assert.AreEqual(0, result.ArchivesRemoved, "The period is inclusive: the day it names is still inside it.");
        Assert.IsTrue(File.Exists(onTheBoundary));
    }

    [TestMethod]
    public void ASourceWithNoPeriodSetKeepsEveryArchive()
    {
        var expired = WriteArchivedPicture(DateTime.Now.AddDays(-4000));

        var result = RunCacheStep(archiveKeepDays: 0);

        Assert.AreEqual(0, result.ArchivesRemoved, "A source the retention period does not govern is left entirely alone.");
        Assert.IsTrue(File.Exists(expired));
    }

    [TestMethod]
    public void AnArchivedFileWhoseNameCarriesNoDayIsLeftAlone()
    {
        var archiveFolder = Path.Combine(_pictureRoot, CollectionFolder, "MMC", AppStoragePaths.ArchiveFolderName);
        Directory.CreateDirectory(archiveFolder);
        var undated = Path.Combine(archiveFolder, "a-note-left-here.png");
        ImageHeaderFixtures.WritePng(undated, 64, 64);

        var result = RunCacheStep();

        Assert.AreEqual(0, result.ArchivesRemoved, "A file this cannot date is kept: too long costs disk, too early costs a picture.");
        Assert.IsTrue(File.Exists(undated));
    }

    [TestMethod]
    public void APictureOutsideAnArchiveFolderIsNeverRemoved()
    {
        var picture = Path.Combine(_pictureRoot, CollectionFolder, "MMC", "MMC-12345.png");
        Directory.CreateDirectory(Path.GetDirectoryName(picture)!);
        ImageHeaderFixtures.WritePng(picture, 64, 64);

        // A picture named as if it had been archived, but sitting where the live pictures are.
        var liveButDated = Path.Combine(_pictureRoot, CollectionFolder, "MMC", $"MMC-12345-{DateTime.Now.AddYears(-1):MM-dd-yyyy}-01.png");
        ImageHeaderFixtures.WritePng(liveButDated, 64, 64);

        var result = RunCacheStep();

        Assert.AreEqual(0, result.ArchivesRemoved, "Only a replaced picture's archived copy ages out; the picture in use never does.");
        Assert.IsTrue(File.Exists(picture));
        Assert.IsTrue(File.Exists(liveButDated));
    }
}
