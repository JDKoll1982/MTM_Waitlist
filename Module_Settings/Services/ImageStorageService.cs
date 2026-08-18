using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;
using Windows.Graphics.Imaging;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageStorageService.
/// Validates uploaded images and copies them to the configured shared image folder.
/// Supports archive versioning, accessible-share checks, and deletion operations.
/// </summary>
public sealed class ImageStorageService : IImageStorageService
{
    private readonly IImageStorageConfigurationResolver _configurationResolver;
    private readonly ILogger<ImageStorageService> _logger;
    private readonly ImageValidationRules _validationRules;

    /// <summary>
    /// Initializes a new ImageStorageService.
    /// </summary>
    /// <param name="configurationResolver">Resolver for effective storage configuration</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <exception cref="ArgumentNullException">If any dependency is null</exception>
    public ImageStorageService(
        IImageStorageConfigurationResolver configurationResolver,
        ILogger<ImageStorageService> logger)
    {
        _configurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationRules = new ImageValidationRules();
    }

    /// <inheritdoc />
    public async Task<ImageValidationResult> ValidateImageAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentNullException(nameof(sourceFilePath), "Source file path cannot be null or empty");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!File.Exists(sourceFilePath))
            {
                return CreateValidationFailure(sourceFilePath, "FILE_NOT_FOUND", "The selected image file could not be found.");
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var extension = Path.GetExtension(sourceFilePath);
            var allowed = _validationRules.AllowedExtensions;

            if (!allowed.Any(a => string.Equals(a, extension, StringComparison.OrdinalIgnoreCase) || string.Equals(a, $".{extension.TrimStart('.')}", StringComparison.OrdinalIgnoreCase)))
            {
                return CreateValidationFailure(sourceFilePath, "UNSUPPORTED_EXTENSION",
                    $"Unsupported file type. Allowed types: {string.Join(", ", allowed)}.");
            }

            if (fileInfo.Length < _validationRules.MinFileSizeBytes)
            {
                return CreateValidationFailure(sourceFilePath, "FILE_TOO_SMALL",
                    $"The selected image is too small. Minimum allowed size is {_validationRules.MinFileSizeBytes} bytes.");
            }

            var maxSize = await _configurationResolver.GetMaxFileSizeBytesAsync().ConfigureAwait(false);
            if (fileInfo.Length > maxSize)
            {
                return CreateValidationFailure(sourceFilePath, "FILE_TOO_LARGE",
                    $"The selected image exceeds the max allowed size of {maxSize} bytes.");
            }

            var (width, height) = await ReadImageDimensionsAsync(sourceFilePath).ConfigureAwait(false);
            var dimensions = $"{width}x{height}";

            if (width < _validationRules.MinDimensionPixels || height < _validationRules.MinDimensionPixels)
            {
                return CreateValidationFailure(sourceFilePath, "DIMENSION_TOO_SMALL",
                    $"The image is too small. Minimum dimensions are {_validationRules.MinDimensionPixels}x{_validationRules.MinDimensionPixels} pixels.");
            }

            if (width > _validationRules.MaxDimensionPixels || height > _validationRules.MaxDimensionPixels)
            {
                return CreateValidationFailure(sourceFilePath, "DIMENSION_TOO_LARGE",
                    $"The image is too large. Maximum dimensions are {_validationRules.MaxDimensionPixels}x{_validationRules.MaxDimensionPixels} pixels.");
            }

            var aspectRatio = width / (double)height;
            var requireSquare = (await _configurationResolver.GetEffectiveConfigurationAsync().ConfigureAwait(false)).RequireSquareAspectRatio;
            if (requireSquare && Math.Abs(aspectRatio - _validationRules.TargetAspectRatio) > _validationRules.AspectRatioTolerance)
            {
                return CreateValidationFailure(sourceFilePath, "NOT_SQUARE",
                    $"The image must be square. Detected aspect ratio is {aspectRatio:F2}.",
                    fileInfo.Length,
                    dimensions,
                    aspectRatio);
            }

            return new ImageValidationResult
            {
                IsValid = true,
                FilePath = sourceFilePath,
                FileSizeBytes = fileInfo.Length,
                ImageDimensions = dimensions,
                AspectRatio = aspectRatio
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "The selected file could not be decoded as an image: {FilePath}", sourceFilePath);
            return CreateValidationFailure(sourceFilePath, "UNREADABLE_IMAGE", "The selected file could not be read as an image.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image validation failed for {FilePath}", sourceFilePath);
            return CreateValidationFailure(sourceFilePath, "VALIDATION_FAILED", ex.Message);
        }
    }

    private static async Task<(int Width, int Height)> ReadImageDimensionsAsync(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            return ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Unable to decode image '{filePath}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ImageStorageResult> CopyImageToStorageAsync(string sourceFilePath, string scope, string itemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentNullException(nameof(sourceFilePath), "Source file path cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentNullException(nameof(itemId), "Item ID cannot be null or empty");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var validation = await ValidateImageAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return new ImageStorageResult
            {
                Success = false,
                SourceFilePath = sourceFilePath,
                ErrorMessage = validation.ErrorMessage,
                ErrorCode = validation.ErrorCode,
                ValidationError = validation,
                StoredFileSizeBytes = 0
            };
        }

        try
        {
            var config = await _configurationResolver.GetEffectiveConfigurationAsync().ConfigureAwait(false);
            var storageRoot = config.SharedFolderPath;
            var shareAccessible = await IsShareAccessibleAsync(cancellationToken).ConfigureAwait(false);
            if (!shareAccessible)
            {
                return new ImageStorageResult
                {
                    Success = false,
                    SourceFilePath = sourceFilePath,
                    ErrorCode = "SHARE_UNREACHABLE",
                    ErrorMessage = $"The image share '{storageRoot}' is unavailable or not writable.",
                    StoredFileSizeBytes = 0
                };
            }

            Directory.CreateDirectory(storageRoot);

            var extension = Path.GetExtension(sourceFilePath);
            var safeScope = SanitizeFileToken(scope);
            var safeItemId = SanitizeFileToken(itemId);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{safeScope}_{safeItemId}_{timestamp}{extension}";
            var targetPath = Path.Combine(storageRoot, fileName);

            if (File.Exists(targetPath) && config.EnableArchiveVersioning)
            {
                var archiveFolder = Path.Combine(storageRoot, "Archive");
                Directory.CreateDirectory(archiveFolder);
                var archivedPath = Path.Combine(archiveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{Path.GetExtension(fileName)}");
                File.Move(targetPath, archivedPath);
            }

            File.Copy(sourceFilePath, targetPath, overwrite: true);

            var finalInfo = new FileInfo(targetPath);
            return new ImageStorageResult
            {
                Success = true,
                StoredFilePath = targetPath,
                SourceFilePath = sourceFilePath,
                StoredFileSizeBytes = finalInfo.Length,
                Warnings = Array.Empty<string>()
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "No write access to the image share for {SourceFilePath}", sourceFilePath);
            return new ImageStorageResult
            {
                Success = false,
                SourceFilePath = sourceFilePath,
                ErrorCode = "ACCESS_DENIED",
                ErrorMessage = "The configured image share is not writable with the current Windows account.",
                StoredFileSizeBytes = 0
            };
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Image copy failed for {SourceFilePath}", sourceFilePath);
            return new ImageStorageResult
            {
                Success = false,
                SourceFilePath = sourceFilePath,
                ErrorCode = "COPY_FAILED",
                ErrorMessage = ex.Message,
                StoredFileSizeBytes = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<ImageStorageResult> ValidateAndStoreImageAsync(string sourceFilePath, string scope, string itemId, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateImageAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return new ImageStorageResult
            {
                Success = false,
                SourceFilePath = sourceFilePath,
                ErrorCode = validation.ErrorCode,
                ErrorMessage = validation.ErrorMessage,
                ValidationError = validation,
                StoredFileSizeBytes = 0
            };
        }

        return await CopyImageToStorageAsync(sourceFilePath, scope, itemId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsShareAccessibleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var sharePath = await _configurationResolver.GetSharedFolderPathAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(sharePath))
            {
                return false;
            }

            if (!Directory.Exists(sharePath))
            {
                Directory.CreateDirectory(sharePath);
            }

            var testFile = Path.Combine(sharePath, "_access_check_.tmp");
            await File.WriteAllTextAsync(testFile, "ok", cancellationToken).ConfigureAwait(false);
            File.Delete(testFile);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image share is not accessible");
            return false;
        }
    }

    /// <inheritdoc />
    public string GetConfiguredSharePath()
    {
        return _configurationResolver.GetSharedFolderPathAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public ImageValidationRules GetValidationRules()
    {
        return _validationRules;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteStoredImageAsync(string storedFilePath, bool moveToArchive = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFilePath))
        {
            throw new ArgumentNullException(nameof(storedFilePath), "Stored file path cannot be null or empty");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(storedFilePath))
        {
            return false;
        }

        try
        {
            if (moveToArchive)
            {
                var archiveFolder = Path.Combine(Path.GetDirectoryName(storedFilePath) ?? string.Empty, "Archive");
                Directory.CreateDirectory(archiveFolder);
                var archivePath = Path.Combine(archiveFolder, $"{Path.GetFileNameWithoutExtension(storedFilePath)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{Path.GetExtension(storedFilePath)}");
                File.Move(storedFilePath, archivePath);
                return true;
            }

            File.Delete(storedFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete or archive image file: {StoredFilePath}", storedFilePath);
            throw new InvalidOperationException($"Failed to delete or archive image file '{storedFilePath}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetAvailableDiskSpaceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var sharePath = await _configurationResolver.GetSharedFolderPathAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(sharePath))
            {
                return -1;
            }

            if (!Directory.Exists(sharePath))
            {
                Directory.CreateDirectory(sharePath);
            }

            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => sharePath.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase));

            return drive?.AvailableFreeSpace ?? -1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to determine available disk space for image share");
            return -1;
        }
    }

    private static ImageValidationResult CreateValidationFailure(
        string filePath,
        string errorCode,
        string errorMessage,
        long fileSizeBytes = 0,
        string? imageDimensions = null,
        double? aspectRatio = null)
    {
        return new ImageValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            ImageDimensions = imageDimensions,
            AspectRatio = aspectRatio
        };
    }

    private static string SanitizeFileToken(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var candidate = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(candidate) ? "item" : candidate.Trim();
    }
}
