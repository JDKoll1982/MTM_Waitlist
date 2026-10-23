using Microsoft.Extensions.Logging.Abstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Settings;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The three hopper size pictures are found under the size word with any extension the application accepts, not
/// under <c>.png</c> alone. The plant's three files are JPEGs, and a pinned extension drew the shared placeholder
/// over artwork that was already sitting in the folder (2026-10-23).
/// </summary>
[TestClass]
public sealed class PartPictureResolverHopperTests
{
    private string _shareRoot = string.Empty;
    private string _hopperRoot = string.Empty;
    private string? _originalCacheRoot;

    [TestInitialize]
    public void TestInitialize()
    {
        _originalCacheRoot = ImageCachePaths.LocalCacheRoot;
        _shareRoot = ImageHeaderFixtures.CreateTemporaryDirectory();
        _hopperRoot = Path.Combine(_shareRoot, PartPictureLayout.WaitlistCollection, PartPictureLayout.HoppersFolder);
        Directory.CreateDirectory(_hopperRoot);
        ImageCachePaths.SetCacheRoot(ImageHeaderFixtures.CreateTemporaryDirectory());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImageCachePaths.SetCacheRoot(_originalCacheRoot);
        ImageHeaderFixtures.DeleteTemporaryDirectory(_shareRoot);
        ImageHeaderFixtures.DeleteTemporaryDirectory(ImageCachePaths.LocalCacheRoot ?? string.Empty);
    }

    /// <summary>
    /// The candidate names a size is looked up under, in one fixed order, so the answer cannot depend on the
    /// folder's enumeration order.
    /// </summary>
    [TestMethod]
    public void Service_PartPictureResolverHopperTests_TheLookupNames_CoverEveryAcceptedExtension()
    {
        CollectionAssert.AreEqual(
            new[] { "Large.png", "Large.jpg", "Large.jpeg" },
            PartPictureLayout.HopperPictureFileNames("large").ToArray(),
            "A hopper size is looked up under every extension the application accepts, preferred one first.");

        Assert.AreEqual(
            0,
            PartPictureLayout.HopperPictureFileNames("Enormous").Count,
            "A word that is not one of the three sizes names no file at all.");
    }

    /// <summary>
    /// A JPEG in the hopper folder resolves, which is what the plant's three files are.
    /// </summary>
    [TestMethod]
    public async Task Service_PartPictureResolverHopperTests_AJpegHopperPictureResolves()
    {
        var jpeg = Path.Combine(_hopperRoot, "Small.jpg");
        ImageHeaderFixtures.WriteJpegHeader(jpeg, 400, 400);

        var resolved = await CreateResolver().ResolvePartPicturePathAsync(PartPictureLayout.HopperSizeScope, "Small");

        Assert.AreEqual(jpeg, resolved, "A hopper picture filed as a JPEG must draw, not fall back to the placeholder.");
    }

    /// <summary>
    /// A PNG and a JPEG of the same size: the PNG is the preferred name and answers first, so a machine holding
    /// both draws the same file every time.
    /// </summary>
    [TestMethod]
    public async Task Service_PartPictureResolverHopperTests_ThePreferredExtensionWinsWhenBothExist()
    {
        var png = Path.Combine(_hopperRoot, "Medium.png");
        ImageHeaderFixtures.WritePng(png, 400, 400);
        ImageHeaderFixtures.WriteJpegHeader(Path.Combine(_hopperRoot, "Medium.jpg"), 400, 400);

        var resolved = await CreateResolver().ResolvePartPicturePathAsync(PartPictureLayout.HopperSizeScope, "Medium");

        Assert.AreEqual(png, resolved, "The lookup order must be fixed rather than following the folder's own order.");
    }

    /// <summary>
    /// No file at all still answers nothing, so the surface draws the one shared placeholder rather than inventing
    /// artwork (FR-079, FR-080).
    /// </summary>
    [TestMethod]
    public async Task Service_PartPictureResolverHopperTests_ASizeWithNoFileAnswersNothing()
    {
        ImageHeaderFixtures.WriteJpegHeader(Path.Combine(_hopperRoot, "Small.jpg"), 400, 400);

        var resolved = await CreateResolver().ResolvePartPicturePathAsync(PartPictureLayout.HopperSizeScope, "Large");

        Assert.IsNull(resolved, "A size with no picture draws the shared placeholder.");
    }

    private PartPictureResolver CreateResolver()
    {
        var configuration = new FakeImageStorageConfigurationResolver
        {
            SharedFolderPath = _shareRoot,
            HopperFolderPath = _hopperRoot,
            CacheEnabled = false,
        };

        return new PartPictureResolver(
            new FakeImageOverrideReadService(),
            configuration,
            NullLogger<PartPictureResolver>.Instance);
    }
}
