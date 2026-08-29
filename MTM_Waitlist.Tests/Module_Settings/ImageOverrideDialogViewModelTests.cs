using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageOverrideDialogViewModelTests
{
    private FakeImageOverrideReadService _readService = null!;
    private FakeMySqlHelperServer _helper = null!;
    private ImageLocationService _imageLocationService = null!;
    private ImageOverrideWriteService _writeService = null!;
    private FakeImageStorageConfigurationResolver _resolver = null!;
    private ImageStorageService _storageService = null!;
    private FakeWorkCenterCatalogService _catalog = null!;
    private string _workingDirectory = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-dialog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _readService = new FakeImageOverrideReadService();
        _helper = new FakeMySqlHelperServer();
        _catalog = new FakeWorkCenterCatalogService();
        _resolver = new FakeImageStorageConfigurationResolver
        {
            SharedFolderPath = Path.Combine(_workingDirectory, "share")
        };

        _imageLocationService = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            new FakeRequestSubtypeDisplayLabelService(),
            _readService,
            _resolver,
            _catalog,
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

    private RequestTypeImagesDialogViewModel CreateRequestTypeViewModel() =>
        new(_imageLocationService, _readService, _writeService, _storageService,
            NullLogger<RequestTypeImagesDialogViewModel>.Instance);

    private RequestSubtypeImagesDialogViewModel CreateSubtypeViewModel() =>
        new(_imageLocationService, _readService, _writeService, _storageService,
            NullLogger<RequestSubtypeImagesDialogViewModel>.Instance);

    private WorkCenterImagesDialogViewModel CreateWorkCenterViewModel() =>
        new(_imageLocationService, _readService, _writeService, _storageService,
            NullLogger<WorkCenterImagesDialogViewModel>.Instance);

    private static IEnumerable<ImageOverrideRow> AllRows(ImageOverrideDialogViewModel viewModel) =>
        viewModel.Groups.SelectMany(g => g.Rows);

    [TestMethod]
    public async Task RequestTypeDialog_LoadsTheEightInventoryRows()
    {
        var viewModel = CreateRequestTypeViewModel();

        await viewModel.LoadAsync();

        var rows = AllRows(viewModel).ToList();
        Assert.AreEqual(RequestTypeInventory.Items.Count, rows.Count);
        CollectionAssert.AreEquivalent(
            RequestTypeInventory.Items.Select(i => i.DisplayName).ToArray(),
            rows.Select(r => r.DisplayName).ToArray());
    }

    [TestMethod]
    public async Task RequestTypeDialog_BindsTheStableGuidAsTheRowKey()
    {
        var viewModel = CreateRequestTypeViewModel();

        await viewModel.LoadAsync();

        foreach (var row in AllRows(viewModel))
        {
            Assert.IsTrue(Guid.TryParse(row.ItemId, out _), $"'{row.ItemId}' is not a GUID.");
        }
    }

    [TestMethod]
    public async Task Dialog_HydratesExistingOverridesIntoTheEditableColumn()
    {
        var first = RequestTypeInventory.Items[0];
        _readService.AddOverride("request_type", first.StableId.ToString(), @"\\share\custom.png");

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(r => r.ItemId == first.StableId.ToString());
        Assert.AreEqual(@"\\share\custom.png", row.CustomPath);
        Assert.IsTrue(row.HasCustomImage);
        Assert.IsFalse(row.IsDirty);
    }

    [TestMethod]
    public async Task SearchFiltersRowsByDisplayName()
    {
        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        viewModel.SearchText = RequestTypeInventory.Items[0].DisplayName;

        Assert.AreEqual(1, AllRows(viewModel).Count());
    }

    [TestMethod]
    public async Task CustomOnlyToggleHidesRowsWithoutAnOverride()
    {
        var first = RequestTypeInventory.Items[0];
        _readService.AddOverride("request_type", first.StableId.ToString(), "custom.png");

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        viewModel.ShowOnlyCustomImages = true;

        Assert.AreEqual(1, AllRows(viewModel).Count());
        Assert.AreEqual(first.DisplayName, AllRows(viewModel).Single().DisplayName);
    }

    [TestMethod]
    public async Task ResetRowClearsTheOverrideButLeavesItUncommitted()
    {
        var first = RequestTypeInventory.Items[0];
        _readService.AddOverride("request_type", first.StableId.ToString(), "custom.png");

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(r => r.ItemId == first.StableId.ToString());
        await viewModel.ResetRowCommand.ExecuteAsync(row);

        Assert.AreEqual(string.Empty, row.CustomPath);
        Assert.IsTrue(row.IsDirty, "Reset is a pending edit until Save runs.");
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count, "Nothing may be written before Save.");
    }

    [TestMethod]
    public async Task ResetAllClearsEveryRow()
    {
        foreach (var item in RequestTypeInventory.Items)
        {
            _readService.AddOverride("request_type", item.StableId.ToString(), "custom.png");
        }

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        await viewModel.ResetAllAsync();

        Assert.IsTrue(AllRows(viewModel).All(r => !r.HasCustomImage));
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count);
    }

    [TestMethod]
    public async Task CancelDiscardsEveryPendingEdit()
    {
        var first = RequestTypeInventory.Items[0];
        _readService.AddOverride("request_type", first.StableId.ToString(), "original.png");

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(r => r.ItemId == first.StableId.ToString());
        row.CustomPath = "edited.png";

        viewModel.CancelEdits();

        Assert.AreEqual("original.png", row.CustomPath);
        Assert.IsFalse(row.IsDirty);
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count);
    }

    [TestMethod]
    public async Task SaveWithNoChangesWritesNothing()
    {
        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        Assert.IsTrue(await viewModel.SaveAsync());
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count);
    }

    [TestMethod]
    public async Task SaveWhenShareIsUnreachableWritesNothingAndReportsTheError()
    {
        _resolver.SharedFolderPath = @"\\mtm-nonexistent-host-for-tests\share";

        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        AllRows(viewModel).First().CustomPath = source;

        Assert.IsFalse(await viewModel.SaveAsync());
        Assert.IsTrue(viewModel.HasError);
        StringAssert.Contains(viewModel.ErrorMessage, "unavailable");
        Assert.AreEqual(0, _helper.ExecutedNonQueries.Count);
    }

    [TestMethod]
    public async Task SaveCopiesTheImageAndPersistsTheStoredPath()
    {
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).First();
        row.CustomPath = source;

        _helper.EnqueueEmptyQueryResult(); // create: existence probe
        _helper.EnqueueNonQueryResult(1);  // create: insert

        Assert.IsTrue(await viewModel.SaveAsync(), viewModel.ErrorMessage);

        var stored = Path.Combine(_resolver.SharedFolderPath, $"request_type_{row.ItemId}.png");
        Assert.IsTrue(File.Exists(stored), "The chosen file must be copied into the share.");
        Assert.AreEqual(stored, row.OriginalPath);
    }

    [TestMethod]
    public async Task SaveRejectsANonSquareImageAndKeepsTheDialogOpen()
    {
        var source = Path.Combine(_workingDirectory, "wide.png");
        TestPngWriter.Write(source, 128, 64);

        var viewModel = CreateRequestTypeViewModel();
        await viewModel.LoadAsync();
        AllRows(viewModel).First().CustomPath = source;

        Assert.IsFalse(await viewModel.SaveAsync());
        StringAssert.Contains(viewModel.ErrorMessage, "square");
    }

    [TestMethod]
    public async Task SubtypeDialog_GroupsRowsByParentRequestType()
    {
        var viewModel = CreateSubtypeViewModel();

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.SupportsGrouping);
        CollectionAssert.AreEquivalent(
            RequestSubtypeInventory.Groups.Select(g => g.ParentDisplayName).ToArray(),
            viewModel.Groups.Select(g => g.Key).ToArray());
    }

    [TestMethod]
    public async Task SubtypeDialog_MarksRowsWithoutAnOverrideAsInherited()
    {
        var viewModel = CreateSubtypeViewModel();

        await viewModel.LoadAsync();

        Assert.IsTrue(AllRows(viewModel).Where(r => r.IsEditable).All(r => r.IsInherited));
    }

    [TestMethod]
    public async Task SubtypeDialog_ClearsTheInheritedBadgeOnceAnOverrideExists()
    {
        var group = RequestSubtypeInventory.Groups.First(g => g.Subtypes.Count > 0);
        var subtype = group.Subtypes[0];
        _readService.AddOverride("request_subtype", subtype.StableId.ToString(), "custom.png");

        var viewModel = CreateSubtypeViewModel();
        await viewModel.LoadAsync();

        var row = AllRows(viewModel).Single(r => r.ItemId == subtype.StableId.ToString());
        Assert.IsFalse(row.IsInherited);
    }

    [TestMethod]
    public async Task SubtypeDialog_ShowsAPlaceholderForParentsWithNoSubtypes()
    {
        var emptyGroups = RequestSubtypeInventory.Groups.Where(g => g.Subtypes.Count == 0).ToList();

        var viewModel = CreateSubtypeViewModel();
        await viewModel.LoadAsync();

        foreach (var group in emptyGroups)
        {
            var rendered = viewModel.Groups.Single(g => g.Key == group.ParentDisplayName);
            Assert.IsTrue(rendered.Rows.Single().IsPlaceholder);
            Assert.IsFalse(rendered.Rows.Single().IsEditable);
        }
    }

    [TestMethod]
    public async Task WorkCenterDialog_GroupsRowsByBuilding()
    {
        _catalog.Catalog = new WorkCenterCatalogResult
        {
            ComputerName = "test",
            HotWorkCenters = new[] { "Press 12" },
            OtherWorkCenters = new[] { "Press 14" }
        };

        _helper.EnqueueQueryResult(
            WorkCenterRow(1, "Press 12", "Expo Drive"),
            WorkCenterRow(2, "Press 14", "Vits Drive"));

        var viewModel = CreateWorkCenterViewModel();
        await viewModel.LoadAsync();

        CollectionAssert.AreEquivalent(
            new[] { "Expo Drive", "Vits Drive" },
            viewModel.Groups.Select(g => g.Key).ToArray());
    }

    [TestMethod]
    public async Task WorkCenterDialog_WhenTheCatalogIsUnavailable_ShowsAnErrorAndDisablesSave()
    {
        // No rows queued, so the catalog lookup yields nothing and the loader reports failure.
        _catalog.Catalog = new WorkCenterCatalogResult { ComputerName = "test" };

        var viewModel = CreateWorkCenterViewModel();
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.HasError);
        Assert.IsFalse(viewModel.CanSave);
        StringAssert.Contains(viewModel.ErrorMessage, "Database unavailable");
    }

    [TestMethod]
    public async Task WorkCenterDialog_DetectsOverridesForWorkCentersThatNoLongerExist()
    {
        _catalog.Catalog = new WorkCenterCatalogResult
        {
            ComputerName = "test",
            OtherWorkCenters = new[] { "Press 12" }
        };

        _helper.EnqueueQueryResult(WorkCenterRow(1, "Press 12", "Expo Drive"));
        _readService.AddOverride("work_center", "999", "orphan.png");

        var viewModel = CreateWorkCenterViewModel();
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.HasOrphanedOverrides);
        CollectionAssert.AreEqual(new[] { "999" }, viewModel.OrphanedItemIds.ToArray());
    }

    private static Dictionary<string, object?> WorkCenterRow(long id, string name, string building) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = id,
            ["workstation_name"] = name,
            ["building"] = building,
            ["sort_rank"] = 100L,
            ["is_active"] = 1
        };
}
