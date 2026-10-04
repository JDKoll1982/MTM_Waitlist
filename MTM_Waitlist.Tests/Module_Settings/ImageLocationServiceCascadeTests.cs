using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageLocationServiceCascadeTests
{
    private string _workingDirectory = string.Empty;
    private FakeImageOverrideReadService _overrides = null!;
    private FakeMySqlHelperServer _mysql = null!;
    private ImageLocationService _service = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _overrides = new FakeImageOverrideReadService();
        _mysql = new FakeMySqlHelperServer();

        _service = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            _overrides,
            new FakeImageStorageConfigurationResolver { SharedFolderPath = _workingDirectory },
            new FakeWorkCenterCatalogService(),
            _mysql);

        await _service.InitializeAsync();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _service?.Dispose();

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    private string CreateImageFile(string name)
    {
        var path = Path.Combine(_workingDirectory, name);
        File.WriteAllText(path, "image");
        return path;
    }

    // ── The Item scope and its Category family (FR-009, contracts/card-and-identifier.md §4, §D10). The
    //    picture is the Item's, falling back to the Category's family and then to the existing placeholder.
    //    The family hop is the same grouping/inheritance the override dialogs already implement through
    //    `SupportsGrouping` / `SupportsInheritance`; expressed at this level it is the Item's catalog
    //    Category, which is what the second read below is keyed on.

    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_KeysTheOverrideByItemCode()
    {
        var overridePath = CreateImageFile("item-override.png");
        _overrides.AddOverride("request_item", "pickup-coil", overridePath);

        var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

        Assert.AreEqual(
            overridePath,
            resolved,
            "An Item's own override is keyed by the Item code the request is stored with.");
    }

    /// <summary>
    /// SC-019: with the shared folder unreachable, a picture is read from the copy this computer holds rather than
    /// shrugged off onto the application's no-picture asset, which is what keeps a screen drawing while the share
    /// is away (FR-042).
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithTheShareUnreachable_UsesTheCopyHeldOnThisComputer()
    {
        var cacheRoot = Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N"));
        var cachedCopy = Path.Combine(cacheRoot, "waitlist", "Waitlist", "request_item", "pickup-coil.png");

        Directory.CreateDirectory(Path.GetDirectoryName(cachedCopy)!);
        File.WriteAllText(cachedCopy, "image");

        ImageCachePaths.SetCacheRoot(cacheRoot);

        try
        {
            // The row names a picture the shared folder does not hold, because the share cannot be reached. The
            // path is relative, which is how the application records one.
            _overrides.AddOverride("request_item", "pickup-coil", "Waitlist/request_item/pickup-coil.png");

            var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

            Assert.AreEqual(
                cachedCopy,
                resolved,
                "the copy held on this computer is the one read when the shared folder cannot be reached (FR-042)");
        }
        finally
        {
            ImageCachePaths.SetCacheRoot(null);

            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-044: a picture that is on neither the shared folder nor this computer is reported as missing rather than
    /// stood in for by another picture. What answers is the application's own no-picture state, which says there is
    /// no picture, and asking for it is an answer rather than a failure — so nothing about a missing picture can
    /// stop a launch.
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithNeitherTheShareNorACopy_AnswersTheNoPictureState()
    {
        ImageCachePaths.SetCacheRoot(Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N")));

        try
        {
            _overrides.AddOverride("request_item", "pickup-coil", "Waitlist/request_item/never-copied.png");

            var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

            Assert.AreEqual(
                ImageLocationDefaults.RequestItemDefaultPath,
                resolved,
                "the application's own no-picture state answers for a picture that is nowhere (FR-044)");
        }
        finally
        {
            ImageCachePaths.SetCacheRoot(null);
        }
    }

    /// <summary>
    /// The Item's own picture wins over its Category's family: the cascade is a preference order, not a merge.
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_ItemOverrideWinsOverTheCategoryFamily()
    {
        var itemPath = CreateImageFile("item-override.png");
        var familyPath = CreateImageFile("family-override.png");
        _overrides.AddOverride("request_item", "pickup-coil", itemPath);
        _overrides.AddOverride("request_category", "Pickup", familyPath);

        var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

        Assert.AreEqual(itemPath, resolved);
    }

    /// <summary>
    /// The second hop: with no Item override, the Item inherits the family image keyed by its Category code,
    /// which is the four-member Category vocabulary rather than the retired eight-entry request-type one.
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithNoItemOverride_InheritsTheCategoryFamilyImage()
    {
        var familyPath = CreateImageFile("family-override.png");
        _overrides.AddOverride("request_category", "Pickup", familyPath);

        var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

        Assert.AreEqual(
            familyPath,
            resolved,
            "The Item must inherit the image its Category family carries.");
    }

    /// <summary>
    /// Each Item inherits its own Category's family, not a neighbouring one: the family key comes from the
    /// catalog, so a Deliver Item cannot be handed the Pickup family.
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_InheritsOnlyItsOwnCategoryFamily()
    {
        var pickupFamilyPath = CreateImageFile("pickup-family.png");
        _overrides.AddOverride("request_category", "Pickup", pickupFamilyPath);

        var resolved = await _service.ResolveRequestItemImagePathAsync("deliver-coil");

        Assert.AreEqual(ImageLocationDefaults.RequestItemDefaultPath, resolved);
    }

    /// <summary>
    /// The third hop: nothing configured anywhere yields the Item scope's own placeholder, which is what the
    /// card refuses to take as a picture (FR-021).
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithNoOverrideAnywhere_ReturnsTheRequestItemPlaceholder()
    {
        var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

        Assert.AreEqual(ImageLocationDefaults.RequestItemDefaultPath, resolved);
        Assert.AreEqual(
            ImageLocationDefaults.RequestItemDefaultPath,
            ImageLocationDefaults.GetDefaultPathByScope("request_item"),
            "The string-keyed lookup must agree with the enum-keyed one for the new scope.");
        Assert.AreEqual(
            ImageLocationDefaults.RequestCategoryDefaultPath,
            ImageLocationDefaults.GetDefaultPathByScope("request_category"),
            "The string-keyed lookup must agree with the enum-keyed one for the new scope.");
    }

    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WhenTheOverrideFileIsMissing_FallsBackToThePlaceholder()
    {
        _overrides.AddOverride("request_item", "pickup-coil", Path.Combine(_workingDirectory, "deleted.png"));

        var resolved = await _service.ResolveRequestItemImagePathAsync("pickup-coil");

        Assert.AreEqual(ImageLocationDefaults.RequestItemDefaultPath, resolved);
    }

    /// <summary>
    /// An Item the catalog does not describe has no family to inherit, so it is not handed a borrowed one. The
    /// family overrides are all present, which is what makes this a check rather than a coincidence.
    /// </summary>
    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithAnUncataloguedItem_DoesNotBorrowAFamily()
    {
        foreach (var category in new[] { "Pickup", "Deliver", "Assist", "Other" })
        {
            _overrides.AddOverride("request_category", category, CreateImageFile($"family-{category}.png"));
        }

        var resolved = await _service.ResolveRequestItemImagePathAsync("no-such-item");

        Assert.AreEqual(
            ImageLocationDefaults.RequestItemDefaultPath,
            resolved,
            "An uncatalogued Item has no Category family, so it must not be given a borrowed image.");
    }

    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_WithAnEmptyItemCode_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ResolveRequestItemImagePathAsync("   "));
    }

    private static Dictionary<string, object?> CatalogRow(string keyColumn, string keyValue, string valueColumn, string value)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            [keyColumn] = keyValue,
            [valueColumn] = value,
        };

    [TestMethod]
    public async Task ResolveWorkCenterImagePathAsync_WithNonNumericId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ResolveWorkCenterImagePathAsync("abc"));
    }

    [TestMethod]
    public async Task ResolveRequestItemImagePathAsync_BeforeInitialization_Throws()
    {
        using var uninitialised = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeImageOverrideReadService(),
            new FakeImageStorageConfigurationResolver(),
            new FakeWorkCenterCatalogService(),
            TestDoubles.CreateUnusedMySqlHelperServer());

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => uninitialised.ResolveRequestItemImagePathAsync("pickup-coil"));
    }

    [TestMethod]
    public async Task RaiseImageLocationUpdated_NotifiesSubscribers()
    {
        ImageLocationChangedEventArgs? received = null;
        using var subscription = _service.SubscribeToImageLocationChanges(args => received = args);

        _service.RaiseImageLocationUpdated("request_item", "pickup-coil");

        Assert.IsNotNull(received);
        Assert.AreEqual("request_item", received!.Scope);
        Assert.AreEqual("pickup-coil", received.ScopeId);
        await Task.CompletedTask;
    }

    [TestMethod]
    public void RaiseImageLocationUpdated_AfterDisposal_DoesNotNotify()
    {
        var notifications = 0;
        var subscription = _service.SubscribeToImageLocationChanges(_ => notifications++);

        _service.RaiseImageLocationUpdated("request_item", "pickup-coil");
        subscription.Dispose();
        _service.RaiseImageLocationUpdated("request_item", "pickup-coil");

        Assert.AreEqual(1, notifications);
    }
}
