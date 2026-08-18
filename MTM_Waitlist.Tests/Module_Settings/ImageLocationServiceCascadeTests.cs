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

        _service = new ImageLocationService(
            NullLogger<ImageLocationService>.Instance,
            new FakeRequestTypeDisplayLabelService(),
            _subtypeLabels,
            _overrides,
            new FakeImageStorageConfigurationResolver { SharedFolderPath = _workingDirectory },
            new FakeWorkCenterCatalogService(),
            TestDoubles.CreateUnusedMySqlHelperServer());

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
