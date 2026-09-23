using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// This computer's copy of one part's picture, made the first time that part is drawn (US7, FR-030 to FR-032).
/// </summary>
/// <remarks>
/// The rules here are the ones a cache gets wrong in the direction nobody notices until a picture is gone: nothing
/// is ever removed, an unreachable source leaves the copy that is already here, and an empty answer from the source
/// is not treated as "the pictures were deleted".
/// </remarks>
[TestClass]
public sealed class PartPictureCacheStoreTests
{
    private const string CollectionFolder = PartPictureLayout.VisualCollection;
    private const string PictureBelowCollection = @"MMC\MMC-12345.png";

    private readonly PartPictureCacheStore _store = new();

    private string _workingDirectory = string.Empty;
    private string _shareRoot = string.Empty;
    private string _cacheRoot = string.Empty;
    private string? _cacheRootBeforeTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = ImageHeaderFixtures.CreateTemporaryDirectory();
        _shareRoot = Path.Combine(_workingDirectory, "share");
        _cacheRoot = Path.Combine(_workingDirectory, "cache");
        Directory.CreateDirectory(_shareRoot);

        // The cache root is one value for the whole process, so the test puts back exactly what it found.
        _cacheRootBeforeTest = ImageCachePaths.LocalCacheRoot;
        ImageCachePaths.SetCacheRoot(_cacheRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImageCachePaths.SetCacheRoot(_cacheRootBeforeTest);

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    private string CachedPath(string collectionFolder = CollectionFolder, string belowCollection = PictureBelowCollection) =>
        ImageCachePaths.CachePathFor(
            ImageCachePaths.PartCollectionCacheFolder(collectionFolder),
            belowCollection);

    /// <summary>Writes the picture on the share and answers its path.</summary>
    private string WriteSharedPicture(int width = 64, int height = 64)
    {
        var path = Path.Combine(_shareRoot, AppStoragePaths.NormalizeSeparators(PictureBelowCollection));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        ImageHeaderFixtures.WritePng(path, width, height);
        return path;
    }

    [TestMethod]
    public void TheFirstDrawCopiesThePictureOntoThisComputer()
    {
        var shared = WriteSharedPicture();

        var copy = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        Assert.AreEqual(CachedPath(), copy, "The answer is the copy, which is what a control then draws.");
        Assert.IsTrue(File.Exists(copy), "The copy is on this computer after the first draw.");
        CollectionAssert.AreEqual(File.ReadAllBytes(shared), File.ReadAllBytes(copy!));
    }

    [TestMethod]
    public void AnUnchangedCopyIsNotCopiedAgain()
    {
        var shared = WriteSharedPicture();
        var copy = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);
        var writtenAt = File.GetLastWriteTimeUtc(copy!);

        var second = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        Assert.AreEqual(copy, second);
        Assert.AreEqual(
            writtenAt,
            File.GetLastWriteTimeUtc(copy!),
            "A part drawn for the second time reads a local file and writes nothing.");
    }

    [TestMethod]
    public void AChangedSourceReplacesTheCopy()
    {
        var shared = WriteSharedPicture();
        _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        // The picture is replaced at the source, which is what a renewed picture looks like from here.
        ImageHeaderFixtures.WritePng(shared, 96, 96);

        var copy = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        Assert.AreEqual(CachedPath(), copy);
        CollectionAssert.AreEqual(
            File.ReadAllBytes(shared),
            File.ReadAllBytes(copy!),
            "A renewed picture is drawn without restarting the application (SC-006).");
    }

    [TestMethod]
    public void AReplyFromASourceThatHasGoneLeavesTheCopyThatIsAlreadyHere()
    {
        var shared = WriteSharedPicture();
        var copy = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);
        File.Delete(shared);

        var answered = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        Assert.AreEqual(
            copy,
            answered,
            "A source that cannot be reached is recorded, and this computer keeps and keeps drawing its copy (FR-031).");
        Assert.IsTrue(File.Exists(copy));
    }

    [TestMethod]
    public void ASourceThatHasGoneAndNoCopyAnswersNothing()
    {
        var missing = Path.Combine(_shareRoot, "not-there.png");

        var answered = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, missing);

        Assert.IsNull(answered, "Nothing was copied, so the caller draws the shared placeholder.");
        Assert.IsFalse(File.Exists(CachedPath()));
    }

    [TestMethod]
    public void AnEmptySourceRemovesNothing()
    {
        var shared = WriteSharedPicture();
        var copy = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, shared);

        var fromNothing = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, null);
        var fromBlank = _store.EnsureLocalCopy(CollectionFolder, PictureBelowCollection, "   ");

        Assert.AreEqual(copy, fromNothing);
        Assert.AreEqual(copy, fromBlank);
        Assert.IsTrue(
            File.Exists(copy),
            "An empty source is not evidence that the pictures were removed, so nothing already copied is deleted (FR-032).");
    }

    [TestMethod]
    public void TheTwoSystemsKeepTheirOwnCopyOfTheSameFileName()
    {
        var shared = WriteSharedPicture();

        var visualCopy = _store.EnsureLocalCopy(PartPictureLayout.VisualCollection, PictureBelowCollection, shared);
        var wipCopy = _store.EnsureLocalCopy(PartPictureLayout.WipCollection, PictureBelowCollection, shared);

        Assert.IsNotNull(visualCopy);
        Assert.IsNotNull(wipCopy);
        Assert.AreNotEqual(
            visualCopy,
            wipCopy,
            "One file name under Visual and under WIP is two copies, so one system's picture can never be the other's (FR-002).");
        Assert.IsTrue(File.Exists(visualCopy));
        Assert.IsTrue(File.Exists(wipCopy));
    }
}
