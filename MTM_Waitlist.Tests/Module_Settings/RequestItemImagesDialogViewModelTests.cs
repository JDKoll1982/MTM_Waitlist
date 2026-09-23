using System.Reflection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// US5 (FR-009, FR-020, FR-021; §D10). The picture screen is keyed by <b>Item</b> and inherits its
/// <b>Category family</b> — the same two-hop shape the subtype dialog already had, re-pointed rather than
/// rebuilt. It is a rename, not a second image subsystem: the storage root, the read/write services and the
/// per-row batched commit are the ones that were already there.
/// </summary>
/// <remarks>
/// FR-020's "the gate" is <b>two</b> gates today: the minutes editor's
/// <c>CanManageUrgencySettings</c> and this screen's <c>CanManageImageLocationSettings</c>. The last check in
/// this class proves they are still two — neither collapsed into one nor replaced by a new one.
/// </remarks>
[TestClass]
public sealed class RequestItemImagesDialogViewModelTests
{
    private FakeImageOverrideReadService _readService = null!;
    private FakeMySqlHelperServer _helper = null!;
    private ImageLocationService _imageLocationService = null!;
    private ImageOverrideWriteService _writeService = null!;
    private FakeImageStorageConfigurationResolver _resolver = null!;
    private ImageStorageService _storageService = null!;
    private string _workingDirectory = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-item-images-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _readService = new FakeImageOverrideReadService();
        _helper = new FakeMySqlHelperServer();
        _resolver = new FakeImageStorageConfigurationResolver
        {
            SharedFolderPath = Path.Combine(_workingDirectory, "share")
        };
        Directory.CreateDirectory(_resolver.SharedFolderPath);

        _imageLocationService = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            _readService,
            _resolver,
            new FakeWorkCenterCatalogService(),
            _helper);

        _writeService = new ImageOverrideWriteService(
            _helper,
            _readService,
            _imageLocationService,
            NullLogger<ImageOverrideWriteService>.Instance);

        _storageService = new ImageStorageService(_resolver, NullLogger<ImageStorageService>.Instance);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _imageLocationService?.Dispose();

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    private RequestItemImagesDialogViewModel CreateViewModel() =>
        new(_imageLocationService, _readService, _writeService, _storageService,
            NullLogger<RequestItemImagesDialogViewModel>.Instance);

    private static IEnumerable<ImageOverrideRow> AllRows(ImageOverrideDialogViewModel viewModel) =>
        viewModel.Groups.SelectMany(group => group.Rows);

    // ── The screen is keyed by Item (§D10) ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Load_KeysOneEditableRowByEachItemsCode()
    {
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        var rows = AllRows(viewModel).Where(row => row.IsEditable).ToList();

        CollectionAssert.AreEquivalent(
            RequestItemCatalog.Items.Select(item => item.Id).ToArray(),
            rows.Select(row => row.ItemId).ToArray(),
            "The picture screen is keyed by Item code — the same identity the request is stored with.");
    }

    [TestMethod]
    public void Scope_IsTheItemScope_NotTheRetiredSubtypeScope()
    {
        var viewModel = CreateViewModel();

        Assert.AreEqual(ImageLocationScope.RequestItem.ToDatabaseString(), viewModel.Scope);
        Assert.AreNotEqual(
            ImageLocationScope.RequestCategory.ToDatabaseString(),
            viewModel.Scope,
            "The picture screen is keyed by Item, not by the Category family the Item inherits from.");
    }

    [TestMethod]
    public async Task Load_GroupsEveryRowUnderItsOwnCategory()
    {
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.SupportsGrouping, "The family the Item inherits from is the Category, so the rows group by it.");

        foreach (var item in RequestItemCatalog.Items)
        {
            var row = AllRows(viewModel).Single(candidate => candidate.ItemId == item.Id);
            Assert.AreEqual(
                RequestItemCatalog.ResolveCategoryName(item.Category),
                row.GroupName,
                $"'{item.Id}' must sit under its own Category.");
        }
    }

    [TestMethod]
    public async Task Load_MarksARowWithNoOverrideOfItsOwnAsInherited()
    {
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        Assert.IsTrue(
            AllRows(viewModel).Where(row => row.IsEditable).All(row => row.IsInherited),
            "An Item with no picture of its own inherits its Category family.");
    }

    // ── FR-021: "nothing configured" never overwrites an image the request already resolves ─────────────

    [TestMethod]
    public async Task ResolveEffectivePath_WhenNothingIsConfiguredForTheItem_KeepsTheInheritedCategoryImage()
    {
        var familyImage = Path.Combine(_resolver.SharedFolderPath, "pickup-family.png");
        TestPngWriter.Write(familyImage, 64, 64);
        _readService.AddOverride(
            ImageLocationScope.RequestCategory.ToDatabaseString(),
            RequestCategory.Pickup.ToString(),
            familyImage);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(candidate => candidate.ItemId == "pickup-coil");

        Assert.AreEqual(
            familyImage,
            row.EffectiveImagePath,
            "An Item with no picture of its own shows the family picture, not 'nothing configured'.");
        Assert.AreNotEqual(
            ImageLocationDefaults.RequestItemDefaultPath,
            row.EffectiveImagePath,
            "The placeholder must not overwrite an image the request already resolves (FR-021).");
        Assert.IsTrue(row.IsInherited);
    }

    [TestMethod]
    public async Task ResolveEffectivePath_WhenTheItemHasItsOwnPicture_ShowsIt()
    {
        var itemImage = Path.Combine(_resolver.SharedFolderPath, "pickup-coil.png");
        TestPngWriter.Write(itemImage, 64, 64);
        _readService.AddOverride(ImageLocationScope.RequestItem.ToDatabaseString(), "pickup-coil", itemImage);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(candidate => candidate.ItemId == "pickup-coil");

        Assert.AreEqual(itemImage, row.EffectiveImagePath, "The Item's own picture wins over the family.");
        Assert.IsFalse(row.IsInherited);
    }

    // ── FR-020: two gates, still two ────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ThePictureScreensGate_IsADifferentPermissionFromTheMinutesEditors()
    {
        var pictures = PermissionRegistry.Find(PermissionKeys.SettingsPartPictures);
        var minutes = PermissionRegistry.Find(PermissionKeys.SettingsUrgencyMinutes);

        Assert.IsNotNull(pictures, "The picture screen's gate must be a declared permission (FR-046).");
        Assert.IsNotNull(minutes, "The minutes editor's gate must be a declared permission.");
        Assert.AreNotEqual(
            pictures!.Key,
            minutes!.Key,
            "The two screens keep two gates: collapsing them would hand the picture screen to somebody who only ever had the minutes.");

        Assert.IsTrue(
            typeof(SettingsViewModel).GetProperty(nameof(SettingsViewModel.CanManageImageLocationSettings)) is not null,
            "The picture screen's gate is CanManageImageLocationSettings.");
        Assert.IsTrue(
            typeof(UrgencyAllotmentEditorViewModel).GetProperty(nameof(UrgencyAllotmentEditorViewModel.CanManageUrgencySettings)) is not null,
            "The minutes editor's gate is CanManageUrgencySettings.");

        // Neither gate is a role list any more: the two types carry no role-name array at all, which is what
        // makes the pair of permissions rather than the pair of lists the two independent gates (FR-054).
        Assert.IsNull(
            typeof(SettingsViewModel).GetField("AllowedImageLocationManageRoles", BindingFlags.NonPublic | BindingFlags.Static),
            "The picture screen's retired role list must be gone rather than left beside its permission.");
        Assert.IsNull(
            typeof(UrgencyAllotmentEditorViewModel).GetField("AllowedUrgencyManageRoles", BindingFlags.NonPublic | BindingFlags.Static),
            "The minutes editor's retired role list must be gone rather than left beside its permission.");
    }
}
