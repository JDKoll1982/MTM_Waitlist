using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Services;

/// <inheritdoc cref="IPartPictureWriteService" />
/// <summary>
/// Sets and replaces one part's picture.
/// </summary>
/// <remarks>
/// <para>
/// The order of the checks is the order of the refusals a person can act on, and each one happens before anything
/// is written: a number past the storage's limit is refused with the limit stated rather than stored truncated; a
/// name that would land on a different part's file is refused naming that part; and only then is the picture
/// validated, copied and recorded.
/// </para>
/// <para>
/// The picture's own path is laid out from the part's number — the collection for its system, the folder for its
/// family, and a file named after the part — and that one value is both where the file is written and what the row
/// records. A row that disagreed with the file's place is a picture the application could never find again.
/// </para>
/// <para>
/// Replacing a picture keeps the one it replaces: the storage service archives it beside its replacement before
/// the new file lands, and the store's own trigger records what the row replaced. Neither is duplicated here.
/// </para>
/// </remarks>
public sealed class PartPictureWriteService : IPartPictureWriteService
{
    private const string HistoryGetProcedure = "sp_config_images_locations_history_get";

    private readonly IImageOverrideReadService _readService;
    private readonly IImageOverrideWriteService _writeService;
    private readonly IImageStorageService _storageService;
    private readonly IImageStorageConfigurationResolver _configurationResolver;
    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly ILogger<PartPictureWriteService> _logger;

    public PartPictureWriteService(
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        IImageStorageConfigurationResolver configurationResolver,
        IMySqlHelperServer mySqlHelperServer,
        ILogger<PartPictureWriteService> logger)
    {
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _writeService = writeService ?? throw new ArgumentNullException(nameof(writeService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _configurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PartPictureSetResult> SetPictureAsync(
        PartPictureSystem system,
        string partNumber,
        string sourceFilePath,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        var scope = system.ToScopeString();
        var normalizedPart = (partNumber ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedPart))
        {
            return Refuse(scope, normalizedPart, "PART_NUMBER_REQUIRED", "A part number is needed before a picture can be set.");
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return Refuse(scope, normalizedPart, "PICTURE_REQUIRED", "Choose a picture before saving.");
        }

        // FR-008: refused with the limit stated, never stored truncated. The limit is the width of the column the
        // part number is keyed by, so a longer value would be silently shortened by the server into a different
        // part's row.
        if (normalizedPart.Length > PartPictureLayout.MaxPartNumberLength)
        {
            return Refuse(
                scope,
                normalizedPart,
                "PART_NUMBER_TOO_LONG",
                $"The part number is {normalizedPart.Length} characters long. The most it may be is {PartPictureLayout.MaxPartNumberLength}.");
        }

        var safeBaseName = PartPictureLayout.SafeFileBaseName(normalizedPart);
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            return Refuse(
                scope,
                normalizedPart,
                "NAME_NOT_USABLE",
                "Every character of this part number is one a file name cannot hold, so it cannot be given a picture.");
        }

        string? collidingPart;
        try
        {
            collidingPart = await FindCollidingPartAsync(scope, normalizedPart, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The parts already pictured under {Scope} could not be read.", scope);
            return Refuse(
                scope,
                normalizedPart,
                "STORE_UNAVAILABLE",
                "The list of parts that already have a picture could not be read, so nothing was saved.");
        }

        if (collidingPart is not null)
        {
            // FR-007: the refusal names the part that already holds the name. Two numbers that differ only by
            // letter case are two parts competing for one file on a file system that cannot tell them apart.
            return Refuse(
                scope,
                normalizedPart,
                "NAME_COLLISION",
                $"The name this picture would be stored under already belongs to part '{collidingPart}'.",
                collidingPart);
        }

        var extension = Path.GetExtension(sourceFilePath);
        var relativePath = PartPictureLayout.RelativePathFor(scope, normalizedPart, extension);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Refuse(scope, normalizedPart, "NAME_NOT_USABLE", "This part cannot be given a picture.");
        }

        var stored = await _storageService
            .CopyImageToRelativePathAsync(sourceFilePath, relativePath, cancellationToken)
            .ConfigureAwait(false);

        if (!stored.Success)
        {
            var code = string.Equals(stored.ErrorCode, "SHARE_UNREACHABLE", StringComparison.OrdinalIgnoreCase)
                ? "SHARE_UNREACHABLE"
                : "VALIDATION_FAILED";

            return Refuse(scope, normalizedPart, code, stored.ErrorMessage ?? "The picture could not be stored.");
        }

        var storedPath = stored.StoredFilePath ?? relativePath;

        ImageOverride? existing;
        try
        {
            existing = await _readService.GetOverrideAsync(scope, normalizedPart, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The picture row for {Scope} part {PartNumber} could not be read.", scope, normalizedPart);
            existing = null;
        }

        var write = existing is null
            ? await _writeService.CreateOverrideAsync(scope, normalizedPart, storedPath, userId, cancellationToken).ConfigureAwait(false)
            : await _writeService.UpdateOverrideAsync(scope, normalizedPart, storedPath, userId, cancellationToken).ConfigureAwait(false);

        if (!write.Success)
        {
            return Refuse(scope, normalizedPart, "STORE_FAILED", write.ErrorMessage ?? "The picture was not recorded.");
        }

        _logger.LogInformation(
            "The picture for {Scope} part {PartNumber} was {Action}.",
            scope,
            normalizedPart,
            existing is null ? "set" : "replaced");

        return new PartPictureSetResult
        {
            Success = true,
            Scope = scope,
            PartNumber = normalizedPart,
            StoredRelativePath = storedPath,
            ReplacedExisting = existing is not null,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PartPictureChangeEntry>> GetChangeRecordAsync(
        PartPictureSystem system,
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        var scope = system.ToScopeString();
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPart))
        {
            return Array.Empty<PartPictureChangeEntry>();
        }

        try
        {
            var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
                HistoryGetProcedure,
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = normalizedPart,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            return rows.Select(MapEntry).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The change record for {Scope} part {PartNumber} could not be read.", scope, normalizedPart);
            return Array.Empty<PartPictureChangeEntry>();
        }
    }

    /// <summary>
    /// The part that already holds the file name this save would use, or null when the name is free.
    /// </summary>
    /// <param name="scope">The part's store scope.</param>
    /// <param name="partNumber">The part number being saved.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The colliding part number, or <see langword="null"/>.</returns>
    /// <remarks>
    /// Compared within the family folder, because two parts in different folders are two files and only a shared
    /// folder makes them rivals. The part's own row is not a collision: saving a picture for a part that already
    /// has one is a replacement, which is what the button is for.
    /// </remarks>
    private async Task<string?> FindCollidingPartAsync(
        string scope,
        string partNumber,
        CancellationToken cancellationToken)
    {
        var existingRows = await _readService.GetOverridesByScopeAsync(scope, cancellationToken).ConfigureAwait(false);
        if (existingRows.Count == 0)
        {
            return null;
        }

        var safeBaseName = PartPictureLayout.SafeFileBaseName(partNumber);
        var familyFolder = PartPictureLayout.FamilyFolderFor(partNumber);

        foreach (var row in existingRows)
        {
            var otherPart = row.ScopeItemId?.Trim() ?? string.Empty;

            // Ordinal on purpose. A number that differs only by letter case is a different part — that is the
            // whole point of the comparison below — while the same number is this part's own row, which is a
            // replacement rather than a collision.
            if (string.IsNullOrWhiteSpace(otherPart) || string.Equals(otherPart, partNumber, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(PartPictureLayout.FamilyFolderFor(otherPart), familyFolder, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(PartPictureLayout.SafeFileBaseName(otherPart), safeBaseName, StringComparison.OrdinalIgnoreCase))
            {
                return otherPart;
            }
        }

        return null;
    }

    private PartPictureSetResult Refuse(
        string scope,
        string partNumber,
        string errorCode,
        string message,
        string? collidingPartNumber = null) =>
        new()
        {
            Success = false,
            Scope = scope,
            PartNumber = partNumber,
            ErrorCode = errorCode,
            ErrorMessage = message,
            CollidingPartNumber = collidingPartNumber,
        };

    private static PartPictureChangeEntry MapEntry(IReadOnlyDictionary<string, object?> row) => new()
    {
        PreviousImagePath = GetString(row, "previous_image_path"),
        NewImagePath = GetString(row, "new_image_path") ?? string.Empty,
        ChangedByUserId = GetNullableInt64(row, "changed_by_user_id"),
        ChangedByDisplayName = GetString(row, "changed_by_display_name") ?? string.Empty,
        ChangedUtc = GetDateTime(row, "changed_utc"),
    };

    private static string? GetString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        var text = value.ToString();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static long? GetNullableInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateTime GetDateTime(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return default;
        }

        return value switch
        {
            DateTime dateTime => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => DateTime.TryParse(
                value.ToString(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed
                : default,
        };
    }
}
