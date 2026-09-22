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
    public void APictureThisComputerHasCopiedIsReadFromTheLocalCopy()
    {
        var cached = WriteCachedPicture(@"request_item\pickup-coil.png");
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        Assert.AreEqual(cached, ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, shared));
    }

    [TestMethod]
    public void APictureThisComputerHasNotCopiedIsReadFromTheShare()
    {
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        // No answer rather than a hopeful path: the caller falls back to the share, and a path with no file behind
        // it would draw nothing at all.
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, shared));
    }

    [TestMethod]
    public void ARelativePathIsLookedUpUnderTheSameFolders()
    {
        var cached = WriteCachedPicture(@"work_center\100-03.png");

        Assert.AreEqual(cached, ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, @"work_center\100-03.png"));
    }

    [TestMethod]
    public void APictureOutsideItsRootHasNoLocalCopy()
    {
        WriteCachedPicture(@"request_item\pickup-coil.png");

        // A picture belonging to another root is stored under that root's folder, so this root can hold no copy of
        // it. Answering with one read from here would be a picture that changes when an unrelated setting does.
        var elsewhere = Path.Combine(_workingDirectory, "another-root", @"request_item\pickup-coil.png");

        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, elsewhere));
    }

    [TestMethod]
    public void WithNoCacheFolderThereIsNothingToPointAt()
    {
        var shared = Path.Combine(_shareRoot, @"request_item\pickup-coil.png");

        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, cacheRoot: null, shared));
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, "   ", shared));
    }

    [TestMethod]
    public void WithNoPictureThereIsNothingToLookUp()
    {
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, path: null));
        Assert.IsNull(ImageCachePaths.TryGetCachedCopy(_shareRoot, _cacheRoot, "  "));
    }

    [TestMethod]
    public void TheCachedCopyKeepsTheFoldersAPictureHasOnTheShare()
    {
        var path = ImageCachePaths.CachePathFor(@"C:\cache", @"request_item\sub\picture.png");

        Assert.AreEqual(Path.Combine(@"C:\cache", "request_item", "sub", "picture.png"), path);
    }
}
