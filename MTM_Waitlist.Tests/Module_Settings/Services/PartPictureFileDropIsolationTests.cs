using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the drop's isolation (FR-002, FR-028): the same file name placed in the other system's collection is not
/// used, because the two systems are kept apart even where they share a part number.
/// </summary>
[TestClass]
public sealed class PartPictureFileDropIsolationTests
{
    private const string SharedPartNumber = "MMC0001000";

    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private PartPictureResolver _resolver = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-drop-isolation", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_sharePath);

        ImageCachePaths.SetCacheRoot(Path.Combine(_workingDirectory, "cache"));

        _resolver = new PartPictureResolver(
            new FakeImageOverrideReadService(),
            new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath },
            NullLogger<PartPictureResolver>.Instance);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImageCachePaths.SetCacheRoot(ImageCachePaths.DefaultCacheRoot);

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task OneNumberDroppedUnderBothSystems_AnswersEachSystemsOwnFile()
    {
        var visual = Drop(PartPictureLayout.VisualCollection);
        var wip = Drop(PartPictureLayout.WipCollection);

        var resolvedVisual = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, SharedPartNumber);
        var resolvedWip = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.WipPartScope, SharedPartNumber);

        Assert.AreEqual(Path.GetFullPath(visual), Path.GetFullPath(resolvedVisual!));
        Assert.AreEqual(Path.GetFullPath(wip), Path.GetFullPath(resolvedWip!));
        Assert.AreNotEqual(resolvedVisual, resolvedWip, "One number, two systems, two pictures.");
    }

    [TestMethod]
    public async Task AFilePlacedUnderOneSystem_IsNotUsedForTheOther()
    {
        Drop(PartPictureLayout.VisualCollection);

        var resolvedWip = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.WipPartScope, SharedPartNumber);

        Assert.IsNull(resolvedWip, "The WIP part draws the shared placeholder rather than the Visual part's picture.");
    }

    [TestMethod]
    public async Task ACopyHeldByOneSystem_IsNotUsedForTheOther()
    {
        // The twin of the defect this feature already fixed once: the copy's folder must name the collection it
        // belongs to, or the two systems' copies collapse into one file.
        var visualCopy = Path.Combine(
            ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.VisualCollection)!,
            PartPictureLayout.CoilFolder,
            SharedPartNumber + ".png");
        TestPngWriter.Write(visualCopy, 64, 64);

        var resolvedVisual = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, SharedPartNumber);
        var resolvedWip = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.WipPartScope, SharedPartNumber);

        Assert.AreEqual(Path.GetFullPath(visualCopy), Path.GetFullPath(resolvedVisual!));
        Assert.IsNull(resolvedWip);
        Assert.AreNotEqual(
            ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.VisualCollection),
            ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.WipCollection));
    }

    private string Drop(string collection)
    {
        var path = Path.Combine(_sharePath, collection, PartPictureLayout.CoilFolder, SharedPartNumber + ".png");
        TestPngWriter.Write(path, 64, 64);
        return path;
    }
}
