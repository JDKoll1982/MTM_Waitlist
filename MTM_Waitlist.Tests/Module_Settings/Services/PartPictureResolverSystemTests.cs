using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The same part number under the two systems is <b>two parts</b>. Each system's picture resolves for its own
/// part only, and the other system's part draws the one shared placeholder — on the share and in this machine's
/// local copies alike (FR-002, FR-013).
/// </summary>
/// <remarks>
/// The check is made on the resolver's own file decision rather than through a screen, because that decision is
/// the whole rule: whichever way a surface reaches it, the answer for one system's part is never the other
/// system's file, and "no picture" is spelled as an empty answer rather than as somebody else's picture.
/// </remarks>
[TestClass]
public sealed class PartPictureResolverSystemTests
{
    private const string SharedPartNumber = "MMC0001000";

    private string _shareRoot = string.Empty;
    private string _cacheRoot = string.Empty;
    private string? _originalCacheRoot;

    [TestInitialize]
    public void TestInitialize()
    {
        _originalCacheRoot = ImageCachePaths.LocalCacheRoot;
        _shareRoot = ImageHeaderFixtures.CreateTemporaryDirectory();
        _cacheRoot = ImageHeaderFixtures.CreateTemporaryDirectory();
        ImageCachePaths.SetCacheRoot(_cacheRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImageCachePaths.SetCacheRoot(_originalCacheRoot);
        ImageHeaderFixtures.DeleteTemporaryDirectory(_shareRoot);
        ImageHeaderFixtures.DeleteTemporaryDirectory(_cacheRoot);
    }

    /// <summary>
    /// Both parts are pictured: each resolves its own file, and neither answers with the other's.
    /// </summary>
    [TestMethod]
    public void EachSystemResolvesItsOwnPicture()
    {
        var visualFile = WriteSharePicture(PartPictureLayout.VisualCollection, SharedPartNumber);
        var wipFile = WriteSharePicture(PartPictureLayout.WipCollection, SharedPartNumber);

        var visual = PartPictureResolver.ResolveUsableFile("visual_part", VisualRelativePath(), _shareRoot);
        var wip = PartPictureResolver.ResolveUsableFile("wip_part", WipRelativePath(), _shareRoot);

        Assert.AreEqual(visualFile, visual, "The Visual part resolves the Visual file.");
        Assert.AreEqual(wipFile, wip, "The WIP part resolves the WIP file.");
        Assert.AreNotEqual(wip, visual, "One number under two systems must never resolve to one file.");
    }

    /// <summary>
    /// Only one system is pictured: the other draws nothing, which is what the shared placeholder stands for.
    /// </summary>
    [TestMethod]
    public void TheUnpicturedSystemsPartDrawsNothing()
    {
        var visualFile = WriteSharePicture(PartPictureLayout.VisualCollection, SharedPartNumber);

        Assert.AreEqual(
            visualFile,
            PartPictureResolver.ResolveUsableFile("visual_part", VisualRelativePath(), _shareRoot),
            "The pictured system draws its own picture.");

        Assert.IsNull(
            PartPictureResolver.ResolveUsableFile("wip_part", WipRelativePath(), _shareRoot),
            "The unpictured system's part must draw the placeholder, not the other system's picture.");
    }

    /// <summary>
    /// This machine has copied the Visual part's picture, and the WIP part still draws nothing: a local copy is
    /// no more shareable between the two systems than the file on the share is (FR-030).
    /// </summary>
    [TestMethod]
    public void ALocalCopyIsNotSharedBetweenTheTwoSystems()
    {
        var visualFile = WriteSharePicture(PartPictureLayout.VisualCollection, SharedPartNumber);

        // The machine's own copy of the Visual part, at the path the cache layout gives it: the collection's own
        // folder, then the part's relative path inside it.
        var visualCopy = Path.Combine(
            ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.VisualCollection)!,
            PartPictureLayout.CoilFolder,
            SharedPartNumber + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(visualCopy)!);
        ImageHeaderFixtures.WritePng(visualCopy, 96, 96);

        Assert.AreEqual(
            visualCopy,
            PartPictureResolver.ResolveUsableFile("visual_part", VisualRelativePath(), _shareRoot),
            "The Visual part draws this machine's copy of its own picture.");

        Assert.IsNotNull(visualFile, "The fixture wrote the Visual part's source file.");

        Assert.IsNull(
            PartPictureResolver.ResolveUsableFile("wip_part", WipRelativePath(), _shareRoot),
            "The WIP part must not be answered with the Visual part's local copy.");
    }

    /// <summary>
    /// The application's own picture scopes keep their kind folders too, so a request-item picture still resolves
    /// under <c>Waitlist/request_item</c> — and when neither the copy nor the source exists, the answer is nothing
    /// rather than a path to a file that is not there.
    /// </summary>
    [TestMethod]
    public void TheApplicationOwnScopeResolvesItsKindFolderAndNothingWhenThereIsNoFile()
    {
        WriteSharePicture(PartPictureLayout.VisualCollection, SharedPartNumber);

        Assert.IsNull(
            PartPictureResolver.ResolveUsableFile("request_item", "Waitlist/request_item/pickup-coil.png", _shareRoot),
            "A picture with no file behind it answers nothing, whatever scope it belongs to.");
    }

    private static string VisualRelativePath() => $"{PartPictureLayout.VisualCollection}/{PartPictureLayout.CoilFolder}/{SharedPartNumber}.png";

    private static string WipRelativePath() => $"{PartPictureLayout.WipCollection}/{PartPictureLayout.CoilFolder}/{SharedPartNumber}.png";

    private string WriteSharePicture(string collectionFolder, string partNumber)
    {
        var path = Path.Combine(_shareRoot, collectionFolder, PartPictureLayout.CoilFolder, partNumber + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        ImageHeaderFixtures.WritePng(path, 96, 96);
        return path;
    }
}
