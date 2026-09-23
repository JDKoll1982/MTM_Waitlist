using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the hand-placed picture (FR-028, FR-029): a file named after its part is drawn for that part with no row
/// recorded for it, and a file the acceptance rule rejects is ignored rather than reported as a fault.
/// </summary>
[TestClass]
public sealed class PartPictureFileDropTests
{
    private const string PartNumber = "MMC0001000";

    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private FakeImageOverrideReadService _readService = null!;
    private PartPictureResolver _resolver = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-drop", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_sharePath);

        // The local cache is a process-wide static, and the drop path is looked for in it first.
        ImageCachePaths.SetCacheRoot(Path.Combine(_workingDirectory, "cache"));

        _readService = new FakeImageOverrideReadService();
        _resolver = new PartPictureResolver(
            _readService,
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
    public async Task AFileNamedAfterThePart_IsDrawnWithNoRowRecordedForIt()
    {
        var dropped = DropSharePicture(PartPictureLayout.VisualCollection, PartPictureLayout.CoilFolder, ".png");

        var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);

        Assert.AreEqual(Path.GetFullPath(dropped), Path.GetFullPath(resolved!));
    }

    [TestMethod]
    public async Task EachAcceptedExtension_IsFound()
    {
        // One part per extension, because a part holding two of them must draw the same one every time and the
        // order that decides it is pinned by the resolver rather than by this test.
        var parts = new (string Extension, string PartNumber)[]
        {
            (".png", "MMC0001000"),
            (".jpg", "MMC0002000"),
            (".jpeg", "MMC0003000"),
        };

        foreach (var (extension, partNumber) in parts)
        {
            var dropped = DropSharePicture(PartPictureLayout.WipCollection, PartPictureLayout.CoilFolder, extension, partNumber);

            var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.WipPartScope, partNumber);

            Assert.AreEqual(Path.GetFullPath(dropped), Path.GetFullPath(resolved!), extension);
        }
    }

    [TestMethod]
    public async Task APartHoldingTwoAcceptedExtensions_DrawsTheSameOneEveryTime()
    {
        var png = DropSharePicture(PartPictureLayout.VisualCollection, PartPictureLayout.CoilFolder, ".png");
        DropSharePicture(PartPictureLayout.VisualCollection, PartPictureLayout.CoilFolder, ".jpg");

        var first = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);
        var second = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);

        Assert.AreEqual(Path.GetFullPath(png), Path.GetFullPath(first!));
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public async Task ANumberNoPrefixRecognises_IsFoundInTheCatchAllFolder()
    {
        var dropped = DropSharePicture(PartPictureLayout.VisualCollection, PartPictureLayout.CatchAllFolder, ".png", "OTHER-1");

        var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, "OTHER-1");

        Assert.AreEqual(Path.GetFullPath(dropped), Path.GetFullPath(resolved!));
    }

    [TestMethod]
    public async Task AFileTheAcceptanceRuleRejects_IsIgnoredRatherThanReported()
    {
        // A strip is not a picture this application draws, and placing one is not a fault to report.
        var rejected = Path.Combine(_sharePath, PartPictureLayout.VisualCollection, PartPictureLayout.CoilFolder, PartNumber + ".png");
        TestPngWriter.Write(rejected, 128, 32);

        var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);

        Assert.IsNull(resolved, "A file the rule rejects is not a picture, so the placeholder is drawn.");
    }

    [TestMethod]
    public async Task AnEmptyFolder_AnswersNothingRatherThanThrowing()
    {
        var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);

        Assert.IsNull(resolved);
    }

    [TestMethod]
    public async Task TheMachinesOwnCopyOfADroppedFile_IsDrawnBeforeTheShare()
    {
        DropSharePicture(PartPictureLayout.VisualCollection, PartPictureLayout.CoilFolder, ".png");

        var copy = Path.Combine(
            ImageCachePaths.PartCollectionCacheFolder(PartPictureLayout.VisualCollection)!,
            PartPictureLayout.CoilFolder,
            PartNumber + ".png");
        TestPngWriter.Write(copy, 64, 64);

        var resolved = await _resolver.ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, PartNumber);

        Assert.AreEqual(
            Path.GetFullPath(copy),
            Path.GetFullPath(resolved!),
            "The copy is looked for at the collection's own cache folder plus the path below the collection.");
    }

    private string DropSharePicture(string collection, string familyFolder, string extension, string? partNumber = null)
    {
        var path = Path.Combine(_sharePath, collection, familyFolder, (partNumber ?? PartNumber) + extension);
        TestPngWriter.Write(path, 64, 64);
        return path;
    }
}
