using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// The local picture cache, and the one rule that decides whether a picture has to be copied again.
/// </summary>
/// <remarks>
/// Half of these tests are about the rule being <em>cheap</em> rather than correct: a cache that copies everything
/// on every startup is slower than no cache at all, because it reads the whole share to write it again. The
/// "not copied again" cases are therefore the point of the class, not a nicety.
/// </remarks>
[TestClass]
public sealed class ImageCacheSyncServiceTests
{
    private string _workingDirectory = string.Empty;
    private string _shareRoot = string.Empty;
    private string _cacheRoot = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-image-cache", Guid.NewGuid().ToString("N"));
        _shareRoot = Path.Combine(_workingDirectory, "share");
        _cacheRoot = Path.Combine(_workingDirectory, "cache");
        Directory.CreateDirectory(_shareRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    private static ImageCacheSource Source(string shareRoot, string cacheRoot) =>
        new("test pictures", shareRoot, cacheRoot);

    /// <summary>Writes a picture on the share at a path relative to the share root.</summary>
    private string WriteSharedPicture(string relativePath, params byte[] bytes)
    {
        var path = Path.Combine(_shareRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes.Length == 0 ? [1, 2, 3, 4] : bytes);
        return path;
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_TheFirstRunCopiesEveryPictureAndItsFolders()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");
        WriteSharedPicture(@"work_center\100-03.jpg");
        WriteSharedPicture(@"request_category\Pickup.jpeg");

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(3, result.Copied, "Every supported picture on the share is copied.");
        Assert.AreEqual(0, result.Removed);
        Assert.AreEqual(0, result.SourcesSkipped.Count);
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")));
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "work_center", "100-03.jpg")));
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "request_category", "Pickup.jpeg")));
    }

    /// <summary>
    /// The rule the whole cache turns on. A copy that matches its original by size and last-written time is left
    /// alone, so a startup with nothing new on the share reads no picture at all.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_AnUnchangedPictureIsNotCopiedAgain()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");

        var first = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);
        var second = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, first.Copied);
        Assert.AreEqual(0, second.Copied, "A picture that has not changed must not be copied a second time.");
        Assert.AreEqual(0, second.Removed);
    }

    /// <summary>
    /// A copy carries the share's own last-written time. Without that stamp the copy would always look newer than
    /// its original and be copied again on every startup for ever.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_ACopyCarriesTheOriginalsLastWrittenTime()
    {
        var shared = WriteSharedPicture(@"request_item\pickup-coil.png");
        var stamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(shared, stamp);

        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(
            stamp,
            File.GetLastWriteTimeUtc(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")),
            "The copy must be stamped with the share's time, which is what makes the next run cheap.");
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_APictureThatChangedSizeIsCopiedAgain()
    {
        var shared = WriteSharedPicture(@"request_item\pickup-coil.png", 1, 2, 3);

        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        File.WriteAllBytes(shared, [1, 2, 3, 4, 5, 6]);
        File.SetLastWriteTimeUtc(shared, File.GetLastWriteTimeUtc(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")));

        var second = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, second.Copied, "A picture of a different size has changed, whatever its time says.");
        Assert.AreEqual(6, new FileInfo(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")).Length);
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_APictureThatWasEditedIsCopiedAgain()
    {
        var shared = WriteSharedPicture(@"request_item\pickup-coil.png");
        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        // Same size, later time: exactly what editing a picture and saving it over the original looks like.
        File.SetLastWriteTimeUtc(shared, DateTime.UtcNow.AddMinutes(5));

        var second = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, second.Copied, "A picture edited in place has changed even though its size has not.");
    }

    /// <summary>The other half of the run: copies whose original is gone are removed, and the folder they left is too.</summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_ACopyWhoseOriginalIsGoneIsRemovedAndItsFolderTidiedAway()
    {
        var shared = WriteSharedPicture(@"request_item\pickup-coil.png");
        WriteSharedPicture(@"work_center\100-03.png");
        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        File.Delete(shared);

        var second = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, second.Removed, "The copy of a deleted picture goes with it.");
        Assert.IsFalse(File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")));
        Assert.IsFalse(
            Directory.Exists(Path.Combine(_cacheRoot, "request_item")),
            "The folder the deletion emptied is removed rather than left behind.");
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "work_center", "100-03.png")));
    }

    /// <summary>
    /// A run that is cancelled stops quietly and answers with what it did: the launch stops this step the moment it
    /// passes its stated maximum, and an exception escaping that cancellation is reported as a crash in a step that
    /// cannot be allowed to fail.
    /// </summary>
    [TestMethod]
    public async Task Service_ImageCacheSyncServiceTests_ACancelledRunAnswersInsteadOfThrowing()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var result = await new ImageCacheSyncService(_ => Task.FromResult<IReadOnlyList<ImageCacheSource>>(
                [Source(_shareRoot, _cacheRoot)]))
            .SynchronizeAsync(cancelled.Token);

        Assert.IsNotNull(result, "a cancelled refresh answers with a result, and never throws out of the task");
    }

    /// <summary>
    /// A cancelled run deletes nothing. The share was only half read, so a copy that has not been compared yet is
    /// not a copy whose original is gone — treating it as one would take pictures away from a machine that is still
    /// using them.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_ACancelledRunDeletesNothingAlreadyCached()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");
        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        // The original is gone, so an uncancelled run would remove this copy.
        File.Delete(Path.Combine(_shareRoot, "request_item", "pickup-coil.png"));

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)], cancelled.Token);

        Assert.AreEqual(0, result.Removed, "a cancelled run removes nothing");
        Assert.AreEqual(0, result.Copied);
        Assert.IsTrue(
            File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")),
            "the copies already held stay exactly where they are");
    }

    /// <summary>
    /// A source that cannot be read — an unreachable share, or one that fails part way — is reported and left
    /// exactly as it is. Emptying the cache on a network blip would take the pictures away at the one moment the
    /// cache is the only thing still working.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_AnUnreachableSourceIsLeftExactlyAsItIs()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");
        ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        var missingShare = Path.Combine(_workingDirectory, "no-such-share");
        var result = ImageCacheSyncService.Synchronize([Source(missingShare, _cacheRoot)]);

        Assert.AreEqual(0, result.Copied);
        Assert.AreEqual(0, result.Removed, "Nothing may be removed while the share cannot be read.");
        CollectionAssert.AreEqual(new[] { "test pictures" }, result.SourcesSkipped.ToArray());
        Assert.IsTrue(
            File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")),
            "The pictures already cached stay put.");
    }

    /// <summary>
    /// A folder the copies cannot be written into is not an unreachable share, and saying so is what tells an
    /// administrator where to look. The share here is perfectly readable; a folder under a file is the same
    /// failure a standard account meets trying to write under <c>Program Files</c>.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_AnUnwritableDestinationIsNotReportedAsAnUnreachableShare()
    {
        WriteSharedPicture(@"Parts\Steel Rack.png");

        var notAFolder = Path.Combine(_workingDirectory, "not-a-folder.txt");
        File.WriteAllText(notAFolder, "x");
        var unusableCache = Path.Combine(notAFolder, "DunnageImages");

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, unusableCache)]);

        Assert.AreEqual(0, result.Copied);
        Assert.AreEqual(
            0,
            result.SourcesSkipped.Count,
            "the share was readable, so it is not the share that could not be read");
        CollectionAssert.AreEqual(
            new[] { "test pictures" },
            result.DestinationsUnavailable.ToArray(),
            "the folder the copies could not be written into is the fault, and it has to be named as one");
    }

    /// <summary>
    /// One picture that will not copy costs that picture and not the rest of the run. Leaving the cache half built
    /// and saying nothing is how a machine comes to hold only a few of the pictures it should.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_APictureThatWillNotCopyDoesNotAbandonTheOthers()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");
        WriteSharedPicture(@"request_item\steel-rack.png");
        WriteSharedPicture(@"request_item\boxes.png");

        // One of the three cannot be written: the name the copy would take is already a folder, which the file
        // system refuses. This is the shape of a picture held open by another program, without needing one.
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "request_item", "steel-rack.png"));

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(2, result.Copied, "the two pictures that could be copied were still copied");
        Assert.AreEqual(1, result.NotCopied, "the one that could not is counted rather than swallowed");
        Assert.AreEqual(0, result.SourcesSkipped.Count);
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.png")));
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "request_item", "boxes.png")));
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_FilesThatAreNotPicturesAreNeitherCopiedNorKept()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");
        WriteSharedPicture(@"notes.txt");
        WriteSharedPicture(@"request_item\pickup-coil.bmp");

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, result.Copied, "Only the picture kinds the image screens accept are cached.");
        Assert.IsFalse(File.Exists(Path.Combine(_cacheRoot, "notes.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(_cacheRoot, "request_item", "pickup-coil.bmp")));
    }

    /// <summary>
    /// The application's own working folder is never mirrored and never pruned: a working file left behind is not
    /// a picture whose original went missing.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_TheApplicationsOwnWorkingFolderIsLeftAlone()
    {
        WriteSharedPicture(@"request_item\pickup-coil.png");

        var workingFile = Path.Combine(_cacheRoot, ImageCachePaths.TempFolderName, "rotated.png");
        Directory.CreateDirectory(Path.GetDirectoryName(workingFile)!);
        File.WriteAllBytes(workingFile, [9, 9, 9]);

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(1, result.Copied);
        Assert.AreEqual(0, result.Removed);
        Assert.IsTrue(File.Exists(workingFile), "The working folder is not the cache's to tidy.");
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_EverySourceIsMirroredIntoItsOwnFolder()
    {
        var otherShare = Path.Combine(_workingDirectory, "other-share");
        Directory.CreateDirectory(otherShare);
        File.WriteAllBytes(Path.Combine(otherShare, "dunnage-part.png"), [7, 7]);

        WriteSharedPicture("waitlist-picture.png");

        var result = ImageCacheSyncService.Synchronize(
        [
            Source(_shareRoot, Path.Combine(_cacheRoot, ImageCachePaths.WaitlistFolderName)),
            new("dunnage pictures", otherShare, Path.Combine(_cacheRoot, ImageCachePaths.DunnageFolderName)),
        ]);

        Assert.AreEqual(2, result.Copied);
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, ImageCachePaths.WaitlistFolderName, "waitlist-picture.png")));
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, ImageCachePaths.DunnageFolderName, "dunnage-part.png")));
    }

    /// <summary>
    /// The cache root is per-user local application data, not a folder beside the executable: the install folder is
    /// not writable for every account that runs the application, and an update must not take the cache with it.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_TheCacheRootIsLocalApplicationData()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MTM_Waitlist",
            "ImageCache");

        Assert.AreEqual(expected, ImageCachePaths.LocalCacheRoot);
    }

    /// <summary>
    /// The receiving application's own Dunnage cache is shared when this account may write in it, and this
    /// application's own folder is used when it may not — never a second copy of the same pictures on one machine,
    /// and never a folder that cannot be written into.
    /// </summary>
    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_TheDunnageCacheIsTheReceivingApplicationsWhenThisAccountMayWriteInIt()
    {
        var chosen = ImageCachePaths.ResolveDunnageCacheFolder();
        var ourOwn = Path.Combine(ImageCachePaths.LocalCacheRoot, ImageCachePaths.DunnageFolderName);

        if (string.Equals(chosen, ourOwn, StringComparison.OrdinalIgnoreCase))
        {
            Assert.IsFalse(
                ImageCachePaths.ReceivingApplicationDunnageCaches.Any(
                    candidate => Directory.Exists(candidate) && ImageCachePaths.IsFolderWritable(candidate)),
                "a receiving-application folder this account may write in has to be the one used");
            return;
        }

        Assert.IsTrue(
            ImageCachePaths.IsFolderWritable(chosen),
            "a folder this account may not write in must never be the one chosen");
    }

    // ── The part collections are left to the part cache store (US7, FR-030) ─────────────────────────────────────

    /// <summary>Writes a picture in one of the shape a run leaves to the part store.</summary>
    private void WritePartCollectionPicture(string collectionFolder, string partFileName)
    {
        var path = Path.Combine(_shareRoot, collectionFolder, "MMC", partFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [5, 5, 5, 5]);
    }

    private ImageCacheSource PictureSource() =>
        new(
            "waitlist pictures",
            _shareRoot,
            _cacheRoot,
            PartPictureLayout.PartCollectionFolders,
            ArchiveKeepDays: 0);

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_TheTwoPartCollectionsAreNotMirroredInOnePassAtStartup()
    {
        WriteSharedPicture(@"Waitlist\request_item\pickup-coil.png");
        WritePartCollectionPicture(PartPictureLayout.VisualCollection, "MMC-1.png");
        WritePartCollectionPicture(PartPictureLayout.WipCollection, "MMC-1.png");

        var result = ImageCacheSyncService.Synchronize([PictureSource()]);

        Assert.AreEqual(
            1,
            result.Copied,
            "Only the application's own pictures are mirrored; a part's picture is copied when that part is first drawn.");
        Assert.IsTrue(File.Exists(Path.Combine(_cacheRoot, "Waitlist", "request_item", "pickup-coil.png")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_cacheRoot, PartPictureLayout.VisualCollection)));
        Assert.IsFalse(Directory.Exists(Path.Combine(_cacheRoot, PartPictureLayout.WipCollection)));
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_APartCollectionCopyLeftByAnEarlierRunIsRemoved()
    {
        WriteSharedPicture(@"Waitlist\request_item\pickup-coil.png");
        WritePartCollectionPicture(PartPictureLayout.VisualCollection, "MMC-1.png");

        var staleCopy = Path.Combine(_cacheRoot, PartPictureLayout.VisualCollection, "MMC", "MMC-1.png");
        Directory.CreateDirectory(Path.GetDirectoryName(staleCopy)!);
        File.WriteAllBytes(staleCopy, [5, 5, 5, 5]);

        var result = ImageCacheSyncService.Synchronize([PictureSource()]);

        Assert.AreEqual(1, result.Removed, "A part picture is the part store's to keep, not the mirror's.");
        Assert.IsFalse(File.Exists(staleCopy));
    }

    [TestMethod]
    public void Service_ImageCacheSyncServiceTests_ACollectionTheSourceDoesNotNameIsStillMirrored()
    {
        WriteSharedPicture(@"Visual\MMC\MMC-1.png");

        var result = ImageCacheSyncService.Synchronize([Source(_shareRoot, _cacheRoot)]);

        Assert.AreEqual(
            1,
            result.Copied,
            "Only a source that names a folder as excluded leaves it alone, so the Dunnage tree is untouched by this rule.");
    }
}
