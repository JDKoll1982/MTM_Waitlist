using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Round-trips real rows through <c>config_images_locations</c> and <c>config_images_locations_history</c> to prove
/// what the unit tests cannot: that the store records a change inside the same statement as the picture it
/// records, and that the recorded-path move rewrites exactly what it reports and reverses exactly.
/// </summary>
/// <remarks>
/// <para>
/// Requires <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c> to point at a schema-provisioned MySQL instance. Without
/// it every test reports inconclusive rather than failing, so the suite stays green offline.
/// </para>
/// <para>
/// The move is exercised under a prefix only this run's rows carry, because a move matches every row that begins
/// with its prefix and a test must never rewrite rows it does not own.
/// </para>
/// </remarks>
[TestClass]
public sealed class ConfigImagesLocationsHistoryIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";
    private const string MoveProcedure = "sp_config_images_locations_paths_move";

    private MySqlHelperServer _helper = null!;
    private ImageOverrideReadService _readService = null!;
    private PartPictureWriteService _pictureWriteService = null!;
    private string _workingDirectory = string.Empty;
    private string _partNumber = string.Empty;
    private string _movePrefix = string.Empty;
    private string _pictureA = string.Empty;
    private string _pictureB = string.Empty;

    [TestInitialize]
    public async Task TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
        _readService = new ImageOverrideReadService(_helper, NullLogger<ImageOverrideReadService>.Instance);

        // A folder of this run's own for the two source pictures, so the live row is written while the company
        // share is never touched.
        _workingDirectory = Path.Combine(Path.GetTempPath(), "mtm-part-picture-live", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        var configurationResolver = new FakeImageStorageConfigurationResolver
        {
            SharedFolderPath = Path.Combine(_workingDirectory, "share"),
        };

        _pictureWriteService = new PartPictureWriteService(
            _readService,
            new ImageOverrideWriteService(
                _helper,
                _readService,
                new ImageLocationService(
                    NullLogger<ImageLocationService>.Instance,
                    _readService,
                    configurationResolver,
                    new FakeWorkCenterCatalogService(),
                    _helper),
                NullLogger<ImageOverrideWriteService>.Instance),
            new ImageStorageService(configurationResolver, NullLogger<ImageStorageService>.Instance),
            configurationResolver,
            _helper,
            NullLogger<PartPictureWriteService>.Instance);

        // Namespaced per run so parallel or interrupted runs never collide with real data.
        var token = Guid.NewGuid().ToString("N");
        _partNumber = $"MMCIT{token[..12].ToUpperInvariant()}";
        _movePrefix = $"itest-{token}";

        _pictureA = Path.Combine(_workingDirectory, "a.png");
        _pictureB = Path.Combine(_workingDirectory, "b.jpg");
        TestPngWriter.Write(_pictureA, 64, 64);
        TestPngWriter.Write(_pictureB, 64, 64);

        await AssertDatabaseReachableAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (_helper is not null && !string.IsNullOrEmpty(_partNumber))
        {
            await _helper.ExecuteSqlNonQueryAsync(
                "DELETE FROM config_images_locations_history WHERE scope_item_id IN (@p_part, @p_move);",
                new Dictionary<string, object?> { ["p_part"] = _partNumber, ["p_move"] = _movePrefix },
                MySqlDatabaseTarget.MtmWaitlist);

            await _helper.ExecuteSqlNonQueryAsync(
                "DELETE FROM config_images_locations WHERE scope_item_id IN (@p_part, @p_move);",
                new Dictionary<string, object?> { ["p_part"] = _partNumber, ["p_move"] = _movePrefix },
                MySqlDatabaseTarget.MtmWaitlist);
        }

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task AFirstPicture_RecordsOneChangeWithNothingBeforeIt()
    {
        var saved = await _pictureWriteService.SetPictureAsync(PartPictureSystem.Visual, _partNumber, _pictureA);
        Assert.IsTrue(saved.Success, saved.ErrorMessage);

        var record = await _pictureWriteService.GetChangeRecordAsync(PartPictureSystem.Visual, _partNumber);

        Assert.AreEqual(1, record.Count, "A first picture is one change.");
        Assert.IsTrue(record[0].IsFirstPicture);
        Assert.IsNull(record[0].PreviousImagePath, "A first picture has nothing before it.");
        Assert.AreEqual(saved.StoredRelativePath, record[0].NewImagePath);
    }

    [TestMethod]
    public async Task AReplacement_RecordsWhatItReplacedAndReadsNewestFirst()
    {
        var first = await _pictureWriteService.SetPictureAsync(PartPictureSystem.Visual, _partNumber, _pictureA);
        Assert.IsTrue(first.Success, first.ErrorMessage);

        var second = await _pictureWriteService.SetPictureAsync(PartPictureSystem.Visual, _partNumber, _pictureB);
        Assert.IsTrue(second.Success, second.ErrorMessage);
        Assert.IsTrue(second.ReplacedExisting);

        var record = await _pictureWriteService.GetChangeRecordAsync(PartPictureSystem.Visual, _partNumber);

        Assert.AreEqual(2, record.Count, "A replacement is one more change, not one fewer.");
        Assert.AreEqual(first.StoredRelativePath, record[0].PreviousImagePath, "The newest entry names what it replaced.");
        Assert.AreEqual(second.StoredRelativePath, record[0].NewImagePath);
        Assert.IsFalse(record[0].IsFirstPicture);
        Assert.IsTrue(record[1].IsFirstPicture, "The oldest entry is the first picture.");
    }

    [TestMethod]
    public async Task TheMove_ReportsTheRowsItRewroteAndReversesExactly()
    {
        const string scope = "request_item";
        var originalPath = $"{_movePrefix}/picture.png";
        var movedPath = $"Waitlist/{_movePrefix}/picture.png";

        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO config_images_locations (public_id, scope, scope_item_id, image_path, is_active, created_utc, updated_utc) VALUES (UUID(), @p_scope, @p_item, @p_path, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());",
            new Dictionary<string, object?>
            {
                ["p_scope"] = scope,
                ["p_item"] = _movePrefix,
                ["p_path"] = originalPath,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        var rowsRewritten = await _helper.ExecuteStoredProcedureNonQueryAsync(
            MoveProcedure,
            new Dictionary<string, object?> { ["p_old_prefix"] = _movePrefix, ["p_new_prefix"] = $"Waitlist/{_movePrefix}" },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, rowsRewritten, "The move reports the rows it rewrote rather than guessing.");
        Assert.AreEqual(movedPath, await ReadPicturePathAsync(_movePrefix));

        var rowsReversed = await _helper.ExecuteStoredProcedureNonQueryAsync(
            MoveProcedure,
            new Dictionary<string, object?> { ["p_old_prefix"] = $"Waitlist/{_movePrefix}", ["p_new_prefix"] = _movePrefix },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(1, rowsReversed, "The reverse rewrites the same rows back.");
        Assert.AreEqual(originalPath, await ReadPicturePathAsync(_movePrefix), "The reverse is exact, not approximate.");
    }

    private async Task<string?> ReadPicturePathAsync(string scopeItemId)
    {
        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT image_path FROM config_images_locations WHERE scope_item_id = @p_item;",
            new Dictionary<string, object?> { ["p_item"] = scopeItemId },
            MySqlDatabaseTarget.MtmWaitlist);

        return rows.Count == 0 ? null : rows[0]["image_path"]?.ToString();
    }

    private async Task AssertDatabaseReachableAsync()
    {
        var rows = await _helper.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) AS c FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name IN ('config_images_locations', 'config_images_locations_history');",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        if (rows.Count == 0 || Convert.ToInt32(rows[0]["c"]) != 2)
        {
            Assert.Inconclusive(
                "config_images_locations and config_images_locations_history must both be present on the configured database; skipping.");
        }
    }
}
