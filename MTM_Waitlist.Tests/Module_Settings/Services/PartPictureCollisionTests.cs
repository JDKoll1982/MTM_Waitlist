using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Pins the three refusals (FR-007, FR-008): a number past the storage's limit is refused with the limit stated and
/// never stored truncated; a name another part already holds is refused naming that part; and two numbers differing
/// only by letter case are refused, because on this machine they would be one file.
/// </summary>
[TestClass]
public sealed class PartPictureCollisionTests
{
    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private FakeImageOverrideReadService _readService = null!;
    private RecordingOverrideWriteService _writer = null!;
    private PartPictureWriteService _service = null!;
    private string _source = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-collision", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_sharePath);

        _source = Path.Combine(_workingDirectory, "picture.png");
        TestPngWriter.Write(_source, 64, 64);

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
    public async Task ANumberPastTheStorageLimit_IsRefusedWithTheLimitStated()
    {
        var tooLong = new string('M', PartPictureLayout.MaxPartNumberLength + 5);

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, tooLong, _source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("PART_NUMBER_TOO_LONG", result.ErrorCode);
        StringAssert.Contains(
            result.ErrorMessage,
            PartPictureLayout.MaxPartNumberLength.ToString(System.Globalization.CultureInfo.CurrentCulture),
            "The refusal has to state the limit, or the person cannot shorten the number to fit it.");
    }

    [TestMethod]
    public async Task ANameAnotherPartHolds_IsRefusedNamingThatPart()
    {
        // Two different numbers whose characters a file name cannot hold are replaced into one name.
        _readService.AddOverride("visual_part", "MMC/0001", "Visual/MMC/MMC_0001.png");
        _writer.Seed("visual_part", "MMC/0001", "Visual/MMC/MMC_0001.png");

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, "MMC:0001", _source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NAME_COLLISION", result.ErrorCode);
        Assert.AreEqual("MMC/0001", result.CollidingPartNumber);
        StringAssert.Contains(result.ErrorMessage, "MMC/0001");
        Assert.AreEqual(0, _writer.Creates.Count);
        Assert.AreEqual(0, _writer.Updates.Count);
        Assert.AreEqual(
            0,
            Directory.GetFiles(_sharePath, "*", SearchOption.AllDirectories).Length,
            "A refused save writes nothing at all, so the part that owns the name keeps its picture.");
    }

    [TestMethod]
    public async Task TwoNumbersDifferingOnlyByCase_AreRefusedRatherThanSharingOneFile()
    {
        _readService.AddOverride("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");
        _writer.Seed("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, "mmc0001000", _source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NAME_COLLISION", result.ErrorCode);
        Assert.AreEqual("MMC0001000", result.CollidingPartNumber);
    }

    [TestMethod]
    public async Task TheSameNumberUnderTheOtherSystem_IsNotACollision()
    {
        _readService.AddOverride("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");
        _writer.Seed("visual_part", "MMC0001000", "Visual/MMC/MMC0001000.png");

        var result = await _service.SetPictureAsync(PartPictureSystem.Wip, "MMC0001000", _source);

        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.AreEqual("WIP/MMC/MMC0001000.png", result.StoredRelativePath);
        Assert.AreEqual(1, _writer.Creates.Count, "A Visual part's picture never stands in for a WIP part's.");
    }

    [TestMethod]
    public async Task ThePartsAlreadyPicturedCannotBeRead_SavesNothing()
    {
        _readService.FailScopeReads = new InvalidOperationException("the store is unreachable");

        var result = await _service.SetPictureAsync(PartPictureSystem.Visual, "MMC0001000", _source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("STORE_UNAVAILABLE", result.ErrorCode);
        Assert.AreEqual(0, _writer.Creates.Count);
    }
}
