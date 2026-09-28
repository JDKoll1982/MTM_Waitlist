using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class ImageStorageConfigurationResolverTests
{
    private const string AppsettingsPath = @"X:\Software Development\Live Applications\MTM_Waitlist\Images";

    /// <summary>A folder this machine's own configuration names, used to stand for what machine setup captured.</summary>
    private const string MachinePath = @"X:\this-machine\Images";

    /// <summary>A folder the plant names, which is the site-wide answer that applies to every computer.</summary>
    private const string PlantPath = @"\\server\images";

    private FakeConfigSettingsValueService _configService = null!;
    private StubMachineConfigurationService _machineConfiguration = null!;
    private ImageStorageConfigurationResolver _resolver = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _configService = new FakeConfigSettingsValueService();
        _machineConfiguration = new StubMachineConfigurationService();
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
            _configService,
            _machineConfiguration);
    }

    [TestMethod]
    public async Task GetSharedFolderPathAsync_WithNoDatabaseOverrideAndNoMachineFolder_UsesAppsettingsValue()
    {
        Assert.AreEqual(AppsettingsPath, await _resolver.GetSharedFolderPathAsync());
    }

    /// <summary>
    /// T155: the folder this machine was configured with at setup is what it reads, rather than one compiled into
    /// the build (FR-025).
    /// </summary>
    [TestMethod]
    public async Task GetSharedFolderPathAsync_WithAMachineFolderAndNoPlantOverride_UsesTheMachinesOwnFolder()
    {
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.SharedFolder, MachinePath);

        Assert.AreEqual(MachinePath, await _resolver.GetSharedFolderPathAsync());
    }

    /// <summary>
    /// A plant-wide decision applies to every computer, so it is the answer even where one machine's own
    /// configuration names a different folder, and the difference is reported rather than hidden (FR-009).
    /// </summary>
    [TestMethod]
    public async Task GetSharedFolderPathAsync_WithBothAMachineFolderAndAPlantOverride_UsesThePlantOverride()
    {
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.SharedFolder, MachinePath);
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, PlantPath);

        Assert.AreEqual(PlantPath, await _resolver.GetSharedFolderPathAsync());
    }

    [TestMethod]
    public async Task GetKeysFolderPathAsync_WithAMachineFolderAndNoPlantOverride_UsesTheMachinesOwnFolder()
    {
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.KeysFolder, MachinePath);

        Assert.AreEqual(MachinePath, await _resolver.GetKeysFolderPathAsync());
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
    public async Task GetSharedFolderResolutionAsync_WithNothingStored_ReportsTheShippedDefaultAndNoMachineFolder()
    {
        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(AppsettingsPath, resolution.FolderPath, "With nothing stored anywhere, the shipped default is what is read.");
        Assert.AreEqual(string.Empty, resolution.MachineFolderPath, "No machine row was read, so there is nothing to name beside it.");
        Assert.IsFalse(resolution.MachineDisagrees, "With nothing to compare, there is no disagreement to report.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithTheMachineAndThePlantAgreeing_ReportsNoDisagreement()
    {
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.SharedFolder, AppsettingsPath);
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, AppsettingsPath);

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(AppsettingsPath, resolution.FolderPath);
        Assert.AreEqual(AppsettingsPath, resolution.MachineFolderPath, "This machine's own folder is named beside it.");
        Assert.IsFalse(resolution.MachineDisagrees, "The two agree, so there is no disagreement to report.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithTheMachineHoldingAnotherFolder_ReportsTheStoredFolderAndTheDisagreement()
    {
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.SharedFolder, MachinePath);
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, PlantPath);

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.AreEqual(
            PlantPath,
            resolution.FolderPath,
            "The plant override wins over this machine's own folder, which is what makes one recorded picture resolve for every computer.");
        Assert.AreEqual(MachinePath, resolution.MachineFolderPath, "This machine's own folder is named beside it.");
        Assert.IsTrue(resolution.MachineDisagrees, "This machine is configured with a folder other than the one in force.");
    }

    [TestMethod]
    public async Task GetSharedFolderResolutionAsync_WithTheSameFolderSpelledDifferently_ReportsNoDisagreement()
    {
        // A share is written with forward slashes in some of this application's files and backslashes in others, and
        // one is not a disagreement about where the pictures are.
        _machineConfiguration.SetFolder(MachineConfigurationSourceKinds.SharedFolder, AppsettingsPath);
        _configService.SetText(ConfigSettingKeys.ImageStorageSharedFolderPath, AppsettingsPath.Replace('\\', '/'));

        var resolution = await _resolver.GetSharedFolderResolutionAsync();

        Assert.IsFalse(
            resolution.MachineDisagrees,
            "The same folder spelled with the other separator is the same folder.");
    }

    /// <summary>
    /// This machine's picture sources, as the machine configuration service answers them. Only the read is needed:
    /// the resolver never writes a machine's configuration.
    /// </summary>
    private sealed class StubMachineConfigurationService : IMachineConfigurationService
    {
        private readonly Dictionary<string, string> _folders = new(StringComparer.Ordinal);

        public void SetFolder(string kind, string path) => _folders[kind] = path;

        public Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new MachineConfigurationState(
                IsConfigured: _folders.Count > 0,
                DisplayName: "Fixture Machine",
                Description: null,
                PictureSources: _folders.Select(pair => new PictureSource(pair.Key, pair.Value)).ToArray(),
                UnconfiguredReason: null));

        public Task<MachineConfigurationSaveResult> SaveAsync(
            MachineConfigurationDraft draft,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The resolver is a reader and never saves a machine's configuration.");

        public Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
            IReadOnlyList<string> whatIsBroken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The resolver is a reader and never resets a machine's configuration.");
    }
}
