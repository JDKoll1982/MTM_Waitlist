using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageLocationServiceCascadeTests
{
    private string _workingDirectory = string.Empty;
    private FakeImageOverrideReadService _overrides = null!;
    private FakeRequestSubtypeDisplayLabelService _subtypeLabels = null!;
    private FakeMySqlHelperServer _mysql = null!;
    private ImageLocationService _service = null!;

    private static readonly Guid RequestTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SubtypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestInitialize]
    public async Task TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _overrides = new FakeImageOverrideReadService();
        _subtypeLabels = new FakeRequestSubtypeDisplayLabelService { ParentRequestTypeId = RequestTypeId };
        _mysql = new FakeMySqlHelperServer();

        _service = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            _subtypeLabels,
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

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_WithNoOverride_ReturnsTheDefaultAsset()
    {
        var resolved = await _service.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString());

        Assert.AreEqual(ImageLocationDefaults.RequestTypeDefaultPath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_WithOverride_ReturnsTheOverridePath()
    {
        var overridePath = CreateImageFile("request-type-override.png");
        _overrides.AddOverride("request_type", RequestTypeId.ToString(), overridePath);

        var resolved = await _service.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString());

        Assert.AreEqual(overridePath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_WhenOverrideFileIsMissing_FallsBackToTheDefaultAsset()
    {
        _overrides.AddOverride("request_type", RequestTypeId.ToString(), Path.Combine(_workingDirectory, "deleted.png"));

        var resolved = await _service.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString());

        Assert.AreEqual(ImageLocationDefaults.RequestTypeDefaultPath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestSubtypeImagePathAsync_WithSubtypeOverride_PrefersTheSubtypeImage()
    {
        var subtypePath = CreateImageFile("subtype-override.png");
        var parentPath = CreateImageFile("parent-override.png");
        _overrides.AddOverride("request_subtype", SubtypeId.ToString(), subtypePath);
        _overrides.AddOverride("request_type", RequestTypeId.ToString(), parentPath);

        var resolved = await _service.ResolveRequestSubtypeImagePathAsync(SubtypeId.ToString());

        Assert.AreEqual(subtypePath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestSubtypeImagePathAsync_WithNoSubtypeOverride_InheritsTheParentImage()
    {
        var parentPath = CreateImageFile("parent-override.png");
        _overrides.AddOverride("request_type", RequestTypeId.ToString(), parentPath);

        var resolved = await _service.ResolveRequestSubtypeImagePathAsync(SubtypeId.ToString());

        Assert.AreEqual(parentPath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestSubtypeImagePathAsync_WithNoOverrideAnywhere_ReturnsTheDefaultAsset()
    {
        var resolved = await _service.ResolveRequestSubtypeImagePathAsync(SubtypeId.ToString());

        Assert.AreEqual(ImageLocationDefaults.RequestSubtypeDefaultPath, resolved);
    }

    [TestMethod]
    public async Task ResolveWorkCenterImagePathAsync_WithOverride_ReturnsTheOverridePath()
    {
        var path = CreateImageFile("work-center-override.png");
        _overrides.AddOverride("work_center", "42", path);

        var resolved = await _service.ResolveWorkCenterImagePathAsync("42");

        Assert.AreEqual(path, resolved);
    }

    [TestMethod]
    public async Task ResolveWorkCenterImagePathAsync_WithNoOverride_ReturnsTheWorkCenterDefaultAsset()
    {
        var resolved = await _service.ResolveWorkCenterImagePathAsync("42");

        Assert.AreEqual(ImageLocationDefaults.WorkCenterDefaultPath, resolved);
    }

    [TestMethod]
    public async Task ResolveWorkCenterImagePathAsync_WhenOverrideFileIsMissing_FallsBackToTheDefaultAsset()
    {
        _overrides.AddOverride("work_center", "42", Path.Combine(_workingDirectory, "gone.png"));

        var resolved = await _service.ResolveWorkCenterImagePathAsync("42");

        Assert.AreEqual(ImageLocationDefaults.WorkCenterDefaultPath, resolved);
    }

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_WithoutAnOverride_ReadsTheCatalogDefaultImagePath()
    {
        var catalogPath = CreateImageFile("catalog-request-type.png");
        _mysql.EnqueueQueryResult(CatalogRow("public_id", RequestTypeId.ToString(), "default_image_path", catalogPath));

        var resolved = await _service.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString());

        Assert.AreEqual(catalogPath, resolved);
        Assert.AreEqual(
            "sp_waitlist_request_types_get",
            _mysql.ExecutedQueries.Single().Sql,
            "The catalog procedure — not Assets/Config/waitlist-request-types.json — is the request-type source (T100).");
    }

    [TestMethod]
    public async Task ResolveRequestSubtypeImagePathAsync_WithoutAnOverride_ReadsTheSubtypeCatalogDefaultImagePath()
    {
        var catalogPath = CreateImageFile("catalog-subtype.png");
        _mysql.EnqueueQueryResult(CatalogRow("public_id", SubtypeId.ToString(), "default_image_path", catalogPath));

        var resolved = await _service.ResolveRequestSubtypeImagePathAsync(SubtypeId.ToString());

        Assert.AreEqual(catalogPath, resolved);
        Assert.AreEqual("sp_waitlist_request_subtypes_get", _mysql.ExecutedQueries.Single().Sql);
    }

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_WhenTheCatalogPathIsMissing_FallsBackToTheDefaultAsset()
    {
        _mysql.EnqueueQueryResult(CatalogRow(
            "public_id",
            RequestTypeId.ToString(),
            "default_image_path",
            Path.Combine(_workingDirectory, "gone.png")));

        var resolved = await _service.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString());

        Assert.AreEqual(ImageLocationDefaults.RequestTypeDefaultPath, resolved);
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
    public async Task ResolveRequestTypeImagePathAsync_WithNonGuidId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ResolveRequestTypeImagePathAsync("not-a-guid"));
    }

    [TestMethod]
    public async Task ResolveWorkCenterImagePathAsync_WithNonNumericId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ResolveWorkCenterImagePathAsync("abc"));
    }

    [TestMethod]
    public async Task ResolveRequestTypeImagePathAsync_BeforeInitialization_Throws()
    {
        using var uninitialised = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            new FakeRequestSubtypeDisplayLabelService(),
            new FakeImageOverrideReadService(),
            new FakeImageStorageConfigurationResolver(),
            new FakeWorkCenterCatalogService(),
            TestDoubles.CreateUnusedMySqlHelperServer());

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => uninitialised.ResolveRequestTypeImagePathAsync(RequestTypeId.ToString()));
    }

    [TestMethod]
    public async Task RaiseImageLocationUpdated_NotifiesSubscribers()
    {
        ImageLocationChangedEventArgs? received = null;
        using var subscription = _service.SubscribeToImageLocationChanges(args => received = args);

        _service.RaiseImageLocationUpdated("request_type", RequestTypeId.ToString());

        Assert.IsNotNull(received);
        Assert.AreEqual("request_type", received!.Scope);
        Assert.AreEqual(RequestTypeId.ToString(), received.ScopeId);
        await Task.CompletedTask;
    }

    [TestMethod]
    public void RaiseImageLocationUpdated_AfterDisposal_DoesNotNotify()
    {
        var notifications = 0;
        var subscription = _service.SubscribeToImageLocationChanges(_ => notifications++);

        _service.RaiseImageLocationUpdated("request_type", RequestTypeId.ToString());
        subscription.Dispose();
        _service.RaiseImageLocationUpdated("request_type", RequestTypeId.ToString());

        Assert.AreEqual(1, notifications);
    }
}
