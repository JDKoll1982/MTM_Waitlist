using System;
using System.IO;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Validation result for an image file.
/// Indicates whether the file passes validation and why it failed (if applicable).
/// </summary>
public sealed class ImageValidationResult
{
    /// <summary>
    /// Indicates if the image passes all validation checks.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed.
    /// Null if valid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Specific validation error code.
    /// Examples: UNSUPPORTED_EXTENSION, FILE_TOO_LARGE, NOT_SQUARE, etc.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// The file path that was validated.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// The file size in bytes (if file was readable).
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// The detected image dimensions (width x height) if readable.
    /// </summary>
    public string? ImageDimensions { get; init; }

    /// <summary>
    /// The detected aspect ratio (width / height).
    /// </summary>
    public double? AspectRatio { get; init; }
}

/// <summary>
/// Represents validation rules for image uploads.
/// Defines acceptable file types, size limits, and aspect ratio requirements.
/// </summary>
public sealed class ImageValidationRules
{
    /// <summary>
    /// Allowed file extensions (without dot).
    /// Default: png, jpg, jpeg
    /// </summary>
    public IReadOnlySet<string> AllowedExtensions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "png",
        "jpg",
        "jpeg"
    };

    /// <summary>
    /// Maximum allowed file size in bytes.
    /// Default: 10 MB (10485760 bytes)
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Minimum allowed file size in bytes.
    /// Default: 1 KB (1024 bytes)
    /// </summary>
    public long MinFileSizeBytes { get; init; } = 1024; // 1 KB

    /// <summary>
    /// Maximum aspect ratio tolerance (width / height).
    /// For square images, this should be very close to 1.0.
    /// Tolerance of 0.01 means aspect must be 0.99 to 1.01 for square.
    /// Default: 0.02 (allows ±2% deviation)
    /// </summary>
    public double AspectRatioTolerance { get; init; } = 0.02;

    /// <summary>
    /// Target aspect ratio (usually 1.0 for square images).
    /// Default: 1.0 (square)
    /// </summary>
    public double TargetAspectRatio { get; init; } = 1.0;

    /// <summary>
    /// Minimum image dimension (width and height must both exceed this).
    /// Default: 48 pixels (preview size)
    /// </summary>
    public int MinDimensionPixels { get; init; } = 48;

    /// <summary>
    /// Maximum image dimension (width and height must not exceed this).
    /// Default: 2048 pixels
    /// </summary>
    public int MaxDimensionPixels { get; init; } = 2048;
}

/// <summary>
/// Result of a file copy operation to the shared network folder.
/// Includes the final file path and any warnings that occurred.
/// </summary>
public sealed class ImageStorageResult
{
    /// <summary>
    /// Indicates if the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The final file path where the image was stored.
    /// Null if the operation failed.
    /// </summary>
    public string? StoredFilePath { get; init; }

    /// <summary>
    /// The original source file path that was copied.
    /// </summary>
    public string SourceFilePath { get; init; } = string.Empty;

    /// <summary>
    /// Error message if the operation failed.
    /// Null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Specific error code for programmatic handling.
    /// Examples: VALIDATION_FAILED, SHARE_UNREACHABLE, DISK_FULL, etc.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Validation result (if validation failed, this explains why).
    /// Null if validation passed.
    /// </summary>
    public ImageValidationResult? ValidationError { get; init; }

    /// <summary>
    /// Warnings that occurred during the operation (non-fatal).
    /// For example: "Archive file was replaced due to same name"
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = new List<string>();

    /// <summary>
    /// The file size in bytes that was stored.
    /// </summary>
    public long StoredFileSizeBytes { get; init; }
}
