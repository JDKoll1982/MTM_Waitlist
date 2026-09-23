using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageStorageConfigurationResolverTests
{
    private const string AppsettingsPath = @"X:\Software Development\Live Applications\MTM_Waitlist\Images";

    private FakeConfigSettingsValueService _configService = null!;
    private ImageStorageConfigurationResolver _resolver = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _configService = new FakeConfigSettingsValueService();
        _resolver = new ImageStorageConfigurationResolver(
            NullLogger<ImageStorageConfigurationResolver>.Instance,
            Options.Create(new ImageStorageOptions
            {
                SharedFolderPath = AppsettingsPath,
                MaxFileSizeBytes = 10 * 1024 * 1024,
                AllowedExtensions = new[] { ".png", ".jpg", ".jpeg" },
                RequireSquareAspectRatio = true,
                EnableArchiveVersioning = true,
                ArchiveKeepDays = 30
            }),
            _configService);
    }

    [TestMethod]
    public async Task GetSharedFolderPathAsync_WithNoDatabaseOverride_UsesAppsettingsValue()
    {
        Assert.AreEqual(AppsettingsPath, await _resolver.GetSharedFolderPathAsync());
    }

    [TestMethod]
    public async Task GetSharedFolderPathAsync_WithDatabaseOverride_PrefersTheDatabaseValue()
    {
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, @"\\server\images");

        Assert.AreEqual(@"\\server\images", await _resolver.GetSharedFolderPathAsync());
    }

    [TestMethod]
    public async Task GetSharedFolderPathAsync_CachesTheResolvedValue()
    {
        await _resolver.GetSharedFolderPathAsync();
        await _resolver.GetSharedFolderPathAsync();
        await _resolver.GetSharedFolderPathAsync();

        Assert.AreEqual(1, _configService.GetCallCount, "The resolver must not re-query the database on every read.");
    }

    [TestMethod]
    public async Task InvalidateCache_ForcesTheNextReadToConsultTheDatabaseAgain()
    {
        await _resolver.GetSharedFolderPathAsync();
        _resolver.InvalidateCache();
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, @"\\server\new-images");

        Assert.AreEqual(@"\\server\new-images", await _resolver.GetSharedFolderPathAsync());
    }

    [TestMethod]
    public async Task GetMaxFileSizeBytesAsync_WithDatabaseOverride_PrefersTheDatabaseValue()
    {
        _configService.SetInt(ConfigSettingKeys.ImageStorageMaxFileSizeBytes, 2048);

        Assert.AreEqual(2048, await _resolver.GetMaxFileSizeBytesAsync());
    }

    [TestMethod]
    public async Task GetMaxFileSizeBytesAsync_WithNoOverride_UsesTheTenMegabyteDefault()
    {
        Assert.AreEqual(10 * 1024 * 1024, await _resolver.GetMaxFileSizeBytesAsync());
    }

    [TestMethod]
    public async Task GetEnableArchiveVersioningAsync_WithDatabaseOverride_PrefersTheDatabaseValue()
    {
        _configService.SetBool(ConfigSettingKeys.ImageStorageEnableArchiveVersioning, false);

        Assert.IsFalse(await _resolver.GetEnableArchiveVersioningAsync());
    }

    [TestMethod]
    public async Task GetEffectiveConfigurationAsync_MergesDatabaseOverridesOverAppsettings()
    {
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, @"\\server\images");
        _configService.SetInt(ConfigSettingKeys.ImageStorageMaxFileSizeBytes, 4096);

        var effective = await _resolver.GetEffectiveConfigurationAsync();

        Assert.AreEqual(@"\\server\images", effective.SharedFolderPath);
        Assert.AreEqual(4096, effective.MaxFileSizeBytes);
        // Extensions and the square requirement are not database-overridable.
        CollectionAssert.AreEqual(new[] { ".png", ".jpg", ".jpeg" }, effective.AllowedExtensions.ToArray());
        Assert.IsTrue(effective.RequireSquareAspectRatio);
    }

    [TestMethod]
    public void Constructor_WithNullConfigService_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new ImageStorageConfigurationResolver(
            NullLogger<ImageStorageConfigurationResolver>.Instance,
            Options.Create(new ImageStorageOptions()),
            null!));
    }

    // ── Which folder is the truth, and the disagreement to report (US6, FR-009, FR-010) ────────────────────────

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithNothingStored_ReportsThisMachinesOwnFolderAndNoDisagreement()
    {
        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(AppsettingsPath, resolution.FolderPath, "With nothing stored, this machine's own folder is the truth.");
        Assert.AreEqual(AppsettingsPath, resolution.MachineFolderPath);
        Assert.IsFalse(resolution.MachineDisagrees, "The two agree, so there is no disagreement to report.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithAStoredFolder_ReportsTheStoredFolderAndTheDisagreement()
    {
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, @"\\server\images");

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(
            @"\\server\images",
            resolution.FolderPath,
            "The store wins over this machine's own file, which is what makes one recorded picture resolve for every computer.");
        Assert.AreEqual(AppsettingsPath, resolution.MachineFolderPath, "The file's own value is named beside it.");
        Assert.IsTrue(resolution.MachineDisagrees, "This machine is configured with a folder other than the one in force.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithTheSameFolderSpelledDifferently_ReportsNoDisagreement()
    {
        // A share is written with forward slashes in some of this application's files and backslashes in others, and
        // one is not a disagreement about where the pictures are.
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, AppsettingsPath.Replace('\\', '/'));

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.IsFalse(
            resolution.MachineDisagrees,
            "The same folder spelled with the other separator is the same folder.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithAStoredFolderThatMatches_ReportsNoDisagreement()
    {
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, AppsettingsPath);

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(AppsettingsPath, resolution.FolderPath);
        Assert.IsFalse(resolution.MachineDisagrees);
    }
}
