using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the picture screen (FR-024 to FR-027): finding a part and seeing the picture a surface would draw,
/// setting or replacing that picture, reading the change record back, and every refusal stated in the person's
/// words rather than as a code.
/// </summary>
[TestClass]
public sealed class PartPictureManagerViewModelTests
{
    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private FakeImageOverrideReadService _readService = null!;
    private RecordingOverrideWriteService _writer = null!;
    private PartPictureWriteService _writeService = null!;
    private PartPictureCoverageService _coverageService = null!;
    private PartPictureManagerViewModel _viewModel = null!;
    private string _source = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-screen", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_sharePath);

        _source = Path.Combine(_workingDirectory, "picture.png");
        TestPngWriter.Write(_source, 64, 64);

        _readService = new FakeImageOverrideReadService();
        _writer = new RecordingOverrideWriteService
        {
            // A real write is visible to the next read; a fake one has to be told to be, or the screen would
            // re-read and find nothing where it had just written.
            Written = (scope, item, path) => _readService.AddOverride(scope, item, path),
        };

        var configurationResolver = new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath };
        var storageService = new ImageStorageService(configurationResolver, NullLogger<ImageStorageService>.Instance);

        _writeService = new PartPictureWriteService(
            _readService,
            _writer,
            storageService,
            configurationResolver,
            new FakeMySqlHelperServer(),
            NullLogger<PartPictureWriteService>.Instance);

        _coverageService = new PartPictureCoverageService(
            [new StubPartNumberSource(PartPictureSystem.Wip, "MMC0001000", "MMC0002000")],
            _readService,
            NullLogger<PartPictureCoverageService>.Instance);

        _viewModel = new PartPictureManagerViewModel(
            new PartPictureResolver(_readService, configurationResolver, NullLogger<PartPictureResolver>.Instance),
            _writeService,
            _coverageService,
            NullLogger<PartPictureManagerViewModel>.Instance);
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
    public async Task FindingAPartWithNoPicture_AnswersTheSharedPlaceholderRatherThanNothing()
    {
        var part = await _Find(PartPictureSystem.Visual, "MMC0001000");

        Assert.IsNotNull(part);
        Assert.AreEqual("visual_part", part!.Scope);
        Assert.AreEqual("MMC0001000", part.PartNumber);
        Assert.AreEqual(ImagePicturePolicy.NoImagePath, part.ImagePath, "An empty answer would draw a blank space.");
        Assert.IsFalse(part.HasPicture);
    }

    [TestMethod]
    public async Task FindingAPartThatHasAPicture_AnswersTheFileASurfaceWouldDraw()
    {
        var stored = Path.Combine(_sharePath, "Visual", "MMC", "MMC0001000.png");
        TestPngWriter.Write(stored, 64, 64);
        _readService.AddOverride("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");

        var part = await _Find(PartPictureSystem.Visual, "MMC0001000");

        Assert.IsTrue(part!.HasPicture);
        Assert.AreEqual(Path.GetFullPath(stored), Path.GetFullPath(part.ImagePath));
    }

    [TestMethod]
    public async Task FindingAPartWithNoNumberSaysWhatIsMissing()
    {
        _viewModel.PartNumberInput = "   ";

        var part = await _viewModel.FindPartAsync();

        Assert.IsNull(part);
        Assert.IsTrue(_viewModel.HasError);
        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.ErrorMessage), "The person is told what is missing.");
        Assert.AreEqual("Settings_PartPictures_Refusal_NotAPart.Text", _viewModel.ErrorMessage);
    }

    [TestMethod]
    public async Task SettingAPicture_StoresItAndSaysSo()
    {
        await _Find(PartPictureSystem.Visual, "MMC0001000");

        var saved = await _viewModel.ApplyPictureAsync(_source);

        Assert.IsTrue(saved);
        Assert.IsFalse(_viewModel.HasError);
        Assert.IsTrue(_viewModel.HasStatus, "A saved picture is confirmed, not left silent.");
        Assert.AreEqual(1, _writer.Creates.Count);
        Assert.IsTrue(_viewModel.CurrentPartHasPicture, "The screen shows the picture the store now holds.");
        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(_sharePath, "Visual", "MMC", "MMC0001000.png")),
            Path.GetFullPath(_viewModel.CurrentPart!.ImagePath));
    }

    [TestMethod]
    public async Task AnOverLongNumber_IsRefusedWithTheLimitStated()
    {
        var tooLong = new string('M', PartPictureLayout.MaxPartNumberLength + 5);
        await _Find(PartPictureSystem.Visual, tooLong);

        var saved = await _viewModel.ApplyPictureAsync(_source);

        Assert.IsFalse(saved);
        Assert.AreEqual(0, _writer.Creates.Count);
        Assert.AreEqual("PART_NUMBER_TOO_LONG", _viewModel.LastRefusalCode);
        Assert.AreEqual(PartPictureLayout.MaxPartNumberLength, _viewModel.LastRefusalLimit);
        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.ErrorMessage), "The person is told, not left guessing.");
    }

    [TestMethod]
    public async Task ANameAnotherPartHolds_IsRefusedNamingThatPart()
    {
        _readService.AddOverride("visual_part", "MMC/0001", "Visual/MMC/MMC_0001.png");
        await _Find(PartPictureSystem.Visual, "MMC:0001");

        var saved = await _viewModel.ApplyPictureAsync(_source);

        Assert.IsFalse(saved);
        Assert.AreEqual("NAME_COLLISION", _viewModel.LastRefusalCode);
        Assert.AreEqual("MMC/0001", _viewModel.LastRefusalPartNumber, "The refusal names the part that owns the name.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.ErrorMessage));
    }

    [TestMethod]
    public async Task TheChangeRecord_IsReadBackWithWhoChangedItAndWhatItReplaced()
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
            });

        var configurationResolver = new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath };
        var writeService = new PartPictureWriteService(
            _readService,
            _writer,
            new ImageStorageService(configurationResolver, NullLogger<ImageStorageService>.Instance),
            configurationResolver,
            helper,
            NullLogger<PartPictureWriteService>.Instance);

        var viewModel = new PartPictureManagerViewModel(
            new PartPictureResolver(_readService, configurationResolver, NullLogger<PartPictureResolver>.Instance),
            writeService,
            _coverageService,
            NullLogger<PartPictureManagerViewModel>.Instance);

        viewModel.SelectedSystem = viewModel.Systems.Single(option => option.System == PartPictureSystem.Visual);
        await viewModel.FindPartAsync("MMC0001000");

        Assert.AreEqual(1, viewModel.ChangeRecord.Count);
        StringAssert.Contains(viewModel.ChangeRecord[0].Title, "Replaced");
        StringAssert.Contains(viewModel.ChangeRecord[0].Detail, "Jane Koll");
        StringAssert.Contains(viewModel.ChangeRecord[0].Detail, "Visual/MMC/MMC0001000.png");
        Assert.IsTrue(viewModel.HasChangeRecord);
    }

    [TestMethod]
    public async Task TheMissingList_IsLoadedCountedAndExportable()
    {
        _readService.AddOverride("wip_part", "MMC0001000", "WIP/MMC/MMC0001000.png");

        await _viewModel.LoadMissingPartsAsync();

        Assert.AreEqual(1, _viewModel.MissingParts.Count);
        Assert.AreEqual("MMC0002000", _viewModel.MissingParts[0].PartNumber);
        Assert.IsTrue(_viewModel.HasMissingParts);
        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.MissingCountText));
        StringAssert.Contains(_viewModel.ExportCsv, "MMC0002000");
    }

    [TestMethod]
    public void TheSystemsTheScreenCannotListPartsFor_AreNamedInOneLine()
    {
        // Only the WIP floor can be listed, so the screen says so rather than leaving the Visual part of the list
        // looking empty, which would read as "nothing to do here". The sentence itself is translated and lives in
        // the resources; what is proved here is that the note is raised because one system cannot be listed.
        Assert.IsFalse(_coverageService.CoveredSystems.Contains(PartPictureSystem.Visual));
        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.CoverageNote));
        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, _viewModel.CoverageNoteVisibility);
    }

    private async Task<PartPictureRow?> _Find(PartPictureSystem system, string partNumber)
    {
        _viewModel.SelectedSystem = _viewModel.Systems.Single(option => option.System == system);
        return await _viewModel.FindPartAsync(partNumber);
    }
}
