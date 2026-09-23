using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the write path (FR-007 to FR-011, FR-027): a first save stores a picture where the part's number puts it
/// and creates the row, a replacement stores it and repoints the row, and a refusal writes nothing at all.
/// </summary>
/// <remarks>
/// The change record is written by the store's own triggers, so the "one history row with a null predecessor" half
/// of FR-027 is proved against a live database in <c>ConfigImagesLocationsHistoryIntegrationTests</c>. What this
/// file proves is the part of the promise a unit test can hold: that a refusal reaches neither the file nor the
/// row, and that a save does.
/// </remarks>
[TestClass]
public sealed class PartPictureWriteServiceTests
{
    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private FakeImageOverrideReadService _readService = null!;
    private RecordingOverrideWriteService _writer = null!;
    private PartPictureWriteService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-write", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_sharePath);

        _readService = new FakeImageOverrideReadService();
        _writer = new RecordingOverrideWriteService();

        var configurationResolver = new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath };
        var storageService = new ImageStorageService(configurationResolver, NullLogger<ImageStorageService>.Instance);

        _service = new PartPictureWriteService(
            _readService,
            _writer,
            storageService,
            configurationResolver,
            new FakeMySqlHelperServer(),
            NullLogger<PartPictureWriteService>.Instance);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task FirstSave_StoresThePictureUnderItsCollectionAndFamilyAndCreatesTheRow()
    {
        var source = WriteSourcePicture("first.png");

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, "MMC0001000", source);

        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.AreEqual("Visual/MMC/MMC0001000.png", result.StoredRelativePath);
        Assert.IsTrue(
            File.Exists(Path.Combine(_sharePath, "Visual", "MMC", "MMC0001000.png")),
            "The file must land where the recorded relative path points.");
        Assert.IsFalse(result.ReplacedExisting);
        Assert.AreEqual(1, _writer.Creates.Count);
        Assert.AreEqual(0, _writer.Updates.Count);
    }

    [TestMethod]
    public async Task SecondSave_RepointsTheRowAndKeepsThePictureItReplaced()
    {
        var first = WriteSourcePicture("first.png");
        var second = WriteSourcePicture("second.png");

        // A real write is visible to the next read; a fake one has to be told to be, or the second save would be
        // read as a first one.
        _writer.Written = (scope, item, path) => _readService.AddOverride(scope, item, path);

        await _service.SetPictureAsync(PartPictureSystem.Wip, "MMC0001000", first);
        var replaced = await _service.SetPictureAsync(PartPictureSystem.Wip, "MMC0001000", second);

        Assert.IsTrue(replaced.Success, replaced.ErrorMessage);
        Assert.IsTrue(replaced.ReplacedExisting);
        Assert.AreEqual(1, _writer.Creates.Count);
        Assert.AreEqual(1, _writer.Updates.Count);

        var archiveFolder = Path.Combine(_sharePath, "WIP", "MMC", AppStoragePaths.ArchiveFolderName);
        Assert.IsTrue(Directory.Exists(archiveFolder), "The replaced picture is kept beside its replacement.");
        Assert.AreEqual(1, Directory.GetFiles(archiveFolder).Length);
    }

    [TestMethod]
    public async Task ARefusedSave_WritesNeitherAFileNorARow()
    {
        var source = WriteSourcePicture("first.png");
        var tooLong = new string('A', PartPictureLayout.MaxPartNumberLength + 1);

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, tooLong, source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("PART_NUMBER_TOO_LONG", result.ErrorCode);
        Assert.AreEqual(0, _writer.Creates.Count);
        Assert.AreEqual(0, _writer.Updates.Count);
        Assert.AreEqual(
            0,
            Directory.GetFiles(_sharePath, "*", SearchOption.AllDirectories).Length,
            "A refused number must not be stored truncated.");
    }

    [TestMethod]
    public async Task APictureThatFailsValidation_WritesNeitherAFileNorARow()
    {
        // Square and large enough is the acceptance rule; a strip is not a picture this application draws.
        var source = Path.Combine(_workingDirectory, "strip.png");
        TestPngWriter.Write(source, 128, 32);

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, "MMC0001000", source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("VALIDATION_FAILED", result.ErrorCode);
        Assert.AreEqual(0, _writer.Creates.Count);
        Assert.AreEqual(
            0,
            Directory.GetFiles(_sharePath, "*", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task ChangeRecord_ReadsWhatTheStoreRecordedNewestFirst()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["previous_image_path"] = "Visual/MMC/MMC0001000.png",
                ["new_image_path"] = "Visual/MMC/MMC0001000.jpg",
                ["changed_by_user_id"] = 7L,
                ["changed_by_display_name"] = "Jane Koll",
                ["changed_utc"] = new DateTime(2026, 9, 23, 15, 0, 0, DateTimeKind.Utc),
            },
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["previous_image_path"] = null,
                ["new_image_path"] = "Visual/MMC/MMC0001000.png",
                ["changed_by_user_id"] = null,
                ["changed_by_display_name"] = null,
                ["changed_utc"] = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc),
            });

        var configurationResolver = new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath };
        var service = new PartPictureWriteService(
            _readService,
            _writer,
            new ImageStorageService(configurationResolver, NullLogger<ImageStorageService>.Instance),
            configurationResolver,
            helper,
            NullLogger<PartPictureWriteService>.Instance);

        var record = await service.GetChangeRecordAsync(PartPictureSystem.Visual, "MMC0001000");

        Assert.AreEqual(2, record.Count);
        Assert.AreEqual("sp_config_images_locations_history_get", helper.ExecutedQueries.Single().Sql);
        Assert.AreEqual("visual_part", helper.ExecutedQueries.Single().Parameters["p_scope"]);
        Assert.AreEqual("MMC0001000", helper.ExecutedQueries.Single().Parameters["p_scope_item_id"]);

        Assert.IsFalse(record[0].IsFirstPicture, "The newest entry is a replacement, so it names what it replaced.");
        Assert.AreEqual("Visual/MMC/MMC0001000.png", record[0].PreviousImagePath);
        Assert.AreEqual("Jane Koll", record[0].ChangedByDisplayName);

        Assert.IsTrue(record[1].IsFirstPicture, "The first picture has nothing before it.");
        Assert.IsNull(record[1].PreviousImagePath);
        Assert.AreEqual(string.Empty, record[1].ChangedByDisplayName, "A retired actor leaves the record, not a hole.");
    }

    private string WriteSourcePicture(string fileName)
    {
        var path = Path.Combine(_workingDirectory, fileName);
        TestPngWriter.Write(path, 64, 64);
        return path;
    }
}
