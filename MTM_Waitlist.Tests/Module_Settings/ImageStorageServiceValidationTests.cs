using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageStorageServiceValidationTests
{
    private string _workingDirectory = string.Empty;
    private FakeImageStorageConfigurationResolver _resolver = null!;
    private ImageStorageService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-image-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _resolver = new FakeImageStorageConfigurationResolver
        {
            SharedFolderPath = Path.Combine(_workingDirectory, "share")
        };

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
    public async Task ValidateImageAsync_WhenFileMissing_ReturnsFileNotFound()
    {
        var missingPath = Path.Combine(_workingDirectory, "does-not-exist.png");

        var result = await _service.ValidateImageAsync(missingPath);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("FILE_NOT_FOUND", result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenExtensionNotAllowed_ReturnsUnsupportedExtension()
    {
        var path = Path.Combine(_workingDirectory, "logo.gif");
        TestPngWriter.Write(path, 64, 64);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("UNSUPPORTED_EXTENSION", result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow(".png")]
    [DataRow(".jpg")]
    [DataRow(".jpeg")]
    public async Task ValidateImageAsync_AllowsEveryApprovedExtension(string extension)
    {
        var path = Path.Combine(_workingDirectory, $"square{extension}");
        TestPngWriter.Write(path, 64, 64);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsTrue(result.IsValid, $"Expected {extension} to be accepted but got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenFileExceedsMaxSize_ReturnsFileTooLarge()
    {
        _resolver.MaxFileSizeBytes = 10 * 1024 * 1024;
        var path = Path.Combine(_workingDirectory, "oversize.png");
        TestPngWriter.Write(path, 64, 64, _resolver.MaxFileSizeBytes + 1024);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("FILE_TOO_LARGE", result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenFileIsAtMaxSize_IsAccepted()
    {
        var path = Path.Combine(_workingDirectory, "boundary.png");
        TestPngWriter.Write(path, 64, 64);
        _resolver.MaxFileSizeBytes = new FileInfo(path).Length;

        var result = await _service.ValidateImageAsync(path);

        Assert.IsTrue(result.IsValid, $"Expected the boundary size to be accepted but got {result.ErrorCode}");
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenImageIsNotSquare_ReturnsNotSquare()
    {
        var path = Path.Combine(_workingDirectory, "wide.png");
        TestPngWriter.Write(path, 128, 64);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("NOT_SQUARE", result.ErrorCode);
        Assert.AreEqual("128x64", result.ImageDimensions);
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenSquareRequirementDisabled_AcceptsNonSquare()
    {
        _resolver.RequireSquareAspectRatio = false;
        var path = Path.Combine(_workingDirectory, "wide.png");
        TestPngWriter.Write(path, 128, 64);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsTrue(result.IsValid, $"Expected non-square to be accepted but got {result.ErrorCode}");
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenBelowMinimumDimensions_ReturnsDimensionTooSmall()
    {
        var path = Path.Combine(_workingDirectory, "tiny.png");
        TestPngWriter.Write(path, 16, 16);

        var result = await _service.ValidateImageAsync(path);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("DIMENSION_TOO_SMALL", result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenFileIsNotAnImage_ReturnsUnreadableImage()
    {
        var path = Path.Combine(_workingDirectory, "not-an-image.png");
        await File.WriteAllTextAsync(path, new string('x', 4096));

        var result = await _service.ValidateImageAsync(path);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("UNREADABLE_IMAGE", result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateImageAsync_WhenPathIsEmpty_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _service.ValidateImageAsync(string.Empty));
    }
}
