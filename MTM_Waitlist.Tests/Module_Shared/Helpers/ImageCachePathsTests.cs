using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Shared.Helpers;

/// <summary>
/// The lookup a screen uses to find the local copy of a picture.
/// </summary>
/// <remarks>
/// The copy engine is tested next door; this is the other half, and the half that makes the cache worth having. A
/// cache nothing reads is a folder of wasted bytes, so the cases below are about a screen reaching the local file
/// when there is one and the share when there is not — never a local path that does not exist, and never a local
/// file belonging to a different root.
/// </remarks>
[TestClass]
public sealed class ImageCachePathsTests
{
    private string _workingDirectory = string.Empty;
    private string _shareRoot = string.Empty;
    private string _cacheRoot = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-cache-paths", Guid.NewGuid().ToString("N"));
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

    private string WriteCachedPicture(string relativePath)
    {
        var path = Path.Combine(_cacheRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [9, 9, 9]);
        return path;
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_APictureThisComputerHasCopiedIsReadFromTheLocalCopy()
    {
        var cached = WriteCachedPicture(@"request_item\pickup-coil.png");
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        Assert.AreEqual(cached, ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, shared));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_APictureThisComputerHasNotCopiedIsReadFromTheShare()
    {
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        // No answer rather than a hopeful path: the caller falls back to the share, and a path with no file behind
        // it would draw nothing at all.
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, shared));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_ARelativePathIsLookedUpUnderTheSameFolders()
    {
        var cached = WriteCachedPicture(@"work_center\100-03.png");

        Assert.AreEqual(cached, ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, @"work_center\100-03.png"));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_APictureOutsideItsRootHasNoLocalCopy()
    {
        WriteCachedPicture(@"request_item\pickup-coil.png");

        // A picture belonging to another root is stored under that root's folder, so this root can hold no copy of
        // it. Answering with one read from here would be a picture that changes when an unrelated setting does.
        var elsewhere = Path.Combine(_workingDirectory, "another-root", @"request_item\pickup-coil.png");

        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, elsewhere));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_WithNoCacheFolderThereIsNothingToPointAt()
    {
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, cacheRoot: null, shared));
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, "   ", shared));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_WithNoPictureThereIsNothingToLookUp()
    {
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, path: null));
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, "  "));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_TheCachedCopyKeepsTheFoldersAPictureHasOnTheShare()
    {
        var path = ImageCachePaths.CachePathFor(@"C:\cache", @"request_item\sub\picture.png");

        Assert.AreEqual(Path.Combine(@"C:\cache", "request_item", "sub", "picture.png"), path);
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_AConfiguredFolderWrittenWithAnEnvironmentVariable_IsResolvedRatherThanTakenLiterally()
    {
        try
        {
            ImageCachePaths.SetCacheRoot(@"%LOCALAPPDATA%\MTM_Waitlist\ImageCache");

            Assert.AreEqual(
                ImageCachePaths.DefaultCacheRoot,
                ImageCachePaths.LocalCacheRoot,
                "A configured %LOCALAPPDATA% folder must resolve to this account's own local application data. " +
                "Taken literally it is a relative path, so the cache is created in a folder named %LOCALAPPDATA% " +
                "beside whatever folder the application happened to be started from — which is how cached pictures " +
                "once ended up committed inside the repository.");
        }
        finally
        {
            ImageCachePaths.SetCacheRoot(null);
        }
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_AConfiguredFolderThatIsNotAbsolute_FallsBackToTheShippedDefault()
    {
        try
        {
            ImageCachePaths.SetCacheRoot(@"MTM_Waitlist\ImageCache");

            Assert.AreEqual(
                ImageCachePaths.DefaultCacheRoot,
                ImageCachePaths.LocalCacheRoot,
                "A folder with no root is resolved against the working directory by the file system, which is what " +
                "puts a cache somewhere nobody chose.");
        }
        finally
        {
            ImageCachePaths.SetCacheRoot(null);
        }
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_AConfiguredAbsoluteFolder_IsAdoptedAsWritten()
    {
        var chosen = Path.Combine(_workingDirectory, "chosen");

        try
        {
            ImageCachePaths.SetCacheRoot(chosen);

            Assert.AreEqual(
                chosen,
                ImageCachePaths.LocalCacheRoot,
                "A folder somebody chose on purpose is still the one used.");
        }
        finally
        {
            ImageCachePaths.SetCacheRoot(null);
        }
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_AFolderThatIsNotThereIsNotWritable()
    {
        Assert.IsFalse(ImageCachePaths.IsFolderWritable(Path.Combine(_workingDirectory, "no-such-folder")));
        Assert.IsFalse(ImageCachePaths.IsFolderWritable(null));
        Assert.IsFalse(ImageCachePaths.IsFolderWritable("   "));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_APathThatIsAFileIsNotAFolderThatCanBeWrittenTo()
    {
        var filePath = Path.Combine(_workingDirectory, "pickup-coil.png");
        File.WriteAllBytes(filePath, [1]);

        Assert.IsFalse(ImageCachePaths.IsFolderWritable(filePath));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_AFolderThisAccountMayWriteInIsWritable()
    {
        Directory.CreateDirectory(_cacheRoot);

        Assert.IsTrue(ImageCachePaths.IsFolderWritable(_cacheRoot));
    }

    [TestMethod]
    public void Helper_ImageCachePathsTests_PreparingACacheFolder_MakesItAndAnswersWhetherCopiesCanGoIn()
    {
        var folder = Path.Combine(_cacheRoot, ImageCachePaths.DunnageFolderName);

        Assert.IsTrue(ImageCachePaths.TryPrepareCacheFolder(folder));
        Assert.IsTrue(Directory.Exists(folder), "preparing a folder has to leave it there");

        var notAFolder = Path.Combine(_workingDirectory, "not-a-folder.txt");
        File.WriteAllText(notAFolder, "x");

        Assert.IsFalse(
            ImageCachePaths.TryPrepareCacheFolder(Path.Combine(notAFolder, "DunnageImages")),
            "a folder that cannot be created is not one copies can go into");
    }

    /// <summary>
    /// The rule that decides where the Dunnage copies go. The receiving application's own cache is shared when
    /// this account may write in it, and this application's own folder is used when it may not — because a folder
    /// under <c>Program Files</c> is readable and not writable for the account the application runs as, and every
    /// copy written into it fails.
    /// </summary>
    [TestMethod]
    public void Helper_ImageCachePathsTests_TheDunnageCacheIsAlwaysAFolderThisAccountCanWriteTo()
    {
        var chosen = ImageCachePaths.ResolveDunnageCacheFolder();
        var ourOwn = Path.Combine(ImageCachePaths.LocalCacheRoot, ImageCachePaths.DunnageFolderName);

        if (string.Equals(chosen, ourOwn, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Assert.IsTrue(
            ImageCachePaths.IsFolderWritable(chosen),
            "the Dunnage copies were pointed at a folder this account may not write to, so every copy would fail");
        CollectionAssert.Contains(
            ImageCachePaths.ReceivingApplicationDunnageCaches.ToArray(),
            chosen,
            "a shared folder has to be the receiving application's own cache rather than somewhere new");
    }

    /// <summary>
    /// The two part collections keep their own cache folders, named for the collections themselves. A Visual part
    /// and a WIP part may share a part number, so a shared folder would make one copy stand for two parts
    /// (FR-002, FR-030).
    /// </summary>
    [TestMethod]
    public void Helper_ImageCachePathsTests_TheTwoPartCollectionsKeepTheirOwnCacheFolders()
    {
        var visual = ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.VisualCollection);
        var wip = ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.WipCollection);

        Assert.AreNotEqual(visual, wip, "The two systems must not share one cache folder.");
        Assert.IsTrue(visual.EndsWith(PartPictureLayout.VisualCollection, StringComparison.Ordinal));
        Assert.IsTrue(wip.EndsWith(PartPictureLayout.WipCollection, StringComparison.Ordinal));

        // The same file name in the two collections is two different files on this machine.
        Assert.AreNotEqual(
            ImageCachePaths.CachePathFor(visual, "MMC/MMC0001000.png"),
            ImageCachePaths.CachePathFor(wip, "MMC/MMC0001000.png"),
            "A number shared by the two systems must resolve to two local copies, never to one file.");
    }
}
