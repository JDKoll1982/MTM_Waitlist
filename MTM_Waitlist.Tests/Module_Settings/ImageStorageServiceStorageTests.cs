using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageStorageServiceStorageTests
{
    private string _workingDirectory = string.Empty;
    private string _sharePath = string.Empty;
    private FakeImageStorageConfigurationResolver _resolver = null!;
    private ImageStorageService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N"));
        _sharePath = Path.Combine(_workingDirectory, "share");
        Directory.CreateDirectory(_workingDirectory);

        _resolver = new FakeImageStorageConfigurationResolver { SharedFolderPath = _sharePath };
        _service = new ImageStorageService(_resolver, NullLogger<ImageStorageService>.Instance);
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
    public async Task CopyImageToStorageAsync_UsesDeterministicNameSoTheActiveFileIsOverwritten()
    {
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        var first = await _service.CopyImageToStorageAsync(source, "request_type", "abc-123");
        var second = await _service.CopyImageToStorageAsync(source, "request_type", "abc-123");

        Assert.IsTrue(first.Success, first.ErrorMessage);
        Assert.IsTrue(second.Success, second.ErrorMessage);
        Assert.AreEqual(first.StoredFilePath, second.StoredFilePath);
        Assert.AreEqual("request_type_abc-123.png", Path.GetFileName(first.StoredFilePath));
        Assert.AreEqual(1, Directory.GetFiles(_sharePath).Length, "The active image must be replaced in place.");
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_ArchivesTheReplacedFileWithADatedName()
    {
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        await _service.CopyImageToStorageAsync(source, "work_center", "42");
        await _service.CopyImageToStorageAsync(source, "work_center", "42");

        var archiveFolder = Path.Combine(_sharePath, "Archive");
        Assert.IsTrue(Directory.Exists(archiveFolder), "Replacing an image must create the Archive folder.");

        var archived = Directory.GetFiles(archiveFolder);
        Assert.AreEqual(1, archived.Length);
        StringAssert.Matches(
            Path.GetFileName(archived[0]),
            new System.Text.RegularExpressions.Regex(@"^work_center_42-\d{2}-\d{2}-\d{4}-01\.png$"));
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_IncrementsTheArchiveSequenceWithinTheSameDay()
    {
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        await _service.CopyImageToStorageAsync(source, "work_center", "42");
        await _service.CopyImageToStorageAsync(source, "work_center", "42");
        await _service.CopyImageToStorageAsync(source, "work_center", "42");

        var archived = Directory.GetFiles(Path.Combine(_sharePath, "Archive"));
        Assert.AreEqual(2, archived.Length);
        CollectionAssert.AllItemsAreUnique(archived);
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_WhenArchivingDisabled_DoesNotCreateArchiveFolder()
    {
        _resolver.EnableArchiveVersioning = false;
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        await _service.CopyImageToStorageAsync(source, "request_type", "abc");
        await _service.CopyImageToStorageAsync(source, "request_type", "abc");

        Assert.IsFalse(Directory.Exists(Path.Combine(_sharePath, "Archive")));
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_WhenShareIsUnreachable_FailsWithoutWriting()
    {
        _resolver.SharedFolderPath = @"\\mtm-nonexistent-host-for-tests\share\images";
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        var result = await _service.CopyImageToStorageAsync(source, "request_type", "abc");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SHARE_UNREACHABLE", result.ErrorCode);
        StringAssert.Contains(result.ErrorMessage!, "unavailable");
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_WhenValidationFails_DoesNotWriteToTheShare()
    {
        var source = Path.Combine(_workingDirectory, "wide.png");
        TestPngWriter.Write(source, 128, 64);

        var result = await _service.CopyImageToStorageAsync(source, "request_type", "abc");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("NOT_SQUARE", result.ErrorCode);
        Assert.IsFalse(Directory.Exists(_sharePath) && Directory.GetFiles(_sharePath).Length > 0);
    }

    [TestMethod]
    public async Task CopyImageToStorageAsync_SanitisesScopeAndItemIdIntoTheFileName()
    {
        var source = Path.Combine(_workingDirectory, "source.png");
        TestPngWriter.Write(source, 64, 64);

        var result = await _service.CopyImageToStorageAsync(source, "request_type", "a/b:c");

        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.AreEqual("request_type_a_b_c.png", Path.GetFileName(result.StoredFilePath));
    }

    [TestMethod]
    public async Task IsShareAccessibleAsync_WhenShareIsUnreachable_ReturnsFalse()
    {
        _resolver.SharedFolderPath = @"\\mtm-nonexistent-host-for-tests\share\images";

        Assert.IsFalse(await _service.IsShareAccessibleAsync());
    }

    [TestMethod]
    public async Task IsShareAccessibleAsync_WhenShareIsWritable_ReturnsTrue()
    {
        Assert.IsTrue(await _service.IsShareAccessibleAsync());
    }
}
