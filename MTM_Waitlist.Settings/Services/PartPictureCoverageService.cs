using System.Text;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// One place the application can name parts, and the system those parts belong to.
/// </summary>
/// <remarks>
/// FR-001 defines the parts that can carry a picture as the parts the application can name, so the
/// missing-picture list is exactly the union of these sources minus the parts that already have one. A source
/// answers with the parts it can name and never guesses at the ones it cannot: an unreadable source answers
/// nothing, and the list reports the systems it could read.
/// </remarks>
public interface IPartNumberSource
{
    /// <summary>The system the parts this source names belong to.</summary>
    PartPictureSystem System { get; }

    /// <summary>The distinct part numbers this source can name right now.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The part numbers, or empty when the source could not be read.</returns>
    Task<IReadOnlyList<string>> GetPartNumbersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The parts the application can name that still have no picture, and the list an operator works through.
/// </summary>
/// <remarks>
/// <para>
/// The list is a subtraction, not a new catalogue: the parts a source can name, minus the rows the picture store
/// already holds for that system. A system whose source could not be read contributes nothing rather than
/// everything, because "we could not read it" and "it has no pictures" are different answers and only one of them
/// is a list of work.
/// </para>
/// <para>
/// The subtraction compares part numbers the way the store keys them — case-insensitively — for the same reason
/// the write path refuses two numbers that differ only by letter case: on this machine they would be one file.
/// </para>
/// </remarks>
public sealed class PartPictureCoverageService
{
    private readonly IReadOnlyList<IPartNumberSource> _sources;
    private readonly IImageOverrideReadService _readService;
    private readonly ILogger<PartPictureCoverageService> _logger;

    public PartPictureCoverageService(
        IEnumerable<IPartNumberSource> sources,
        IImageOverrideReadService readService,
        ILogger<PartPictureCoverageService> logger)
    {
        _sources = sources?.ToList() ?? throw new ArgumentNullException(nameof(sources));
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The systems this service can currently list parts for.</summary>
    public IReadOnlyList<PartPictureSystem> CoveredSystems =>
        _sources.Select(source => source.System).Distinct().ToList();

    /// <summary>
    /// The parts the application can name that have no picture, in the sources' own order.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>One row per part still to be photographed; empty when everything is pictured or nothing could be read.</returns>
    public async Task<IReadOnlyList<PartPictureMissRow>> GetMissingPartsAsync(CancellationToken cancellationToken = default)
    {
        var missing = new List<PartPictureMissRow>();

        foreach (var source in _sources)
        {
            var scope = source.System.ToScopeString();

            IReadOnlyList<string> named;
            try
            {
                named = await source.GetPartNumbersAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The parts the application can name for {Scope} could not be read.", scope);
                continue;
            }

            if (named.Count == 0)
            {
                continue;
            }

            HashSet<string> pictured;
            try
            {
                var rows = await _readService.GetOverridesByScopeAsync(scope, cancellationToken).ConfigureAwait(false);
                pictured = new HashSet<string>(
                    rows.Select(row => row.ScopeItemId?.Trim() ?? string.Empty).Where(id => id.Length > 0),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The pictures already stored for {Scope} could not be read.", scope);
                continue;
            }

            foreach (var partNumber in named
                         .Select(value => value?.Trim() ?? string.Empty)
                         .Where(value => value.Length > 0)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (pictured.Contains(partNumber))
                {
                    continue;
                }

                missing.Add(new PartPictureMissRow
                {
                    Scope = scope,
                    SystemName = SystemNameFor(source.System),
                    PartNumber = partNumber,
                    FamilyFolder = PartPictureLayout.FamilyFolderFor(partNumber),
                    RelativePath = RelativeFolderAndNameFor(scope, partNumber),
                });
            }
        }

        return missing;
    }

    /// <summary>
    /// The list as the file the export writes: a header and one line per part.
    /// </summary>
    /// <param name="rows">The rows to write, as <see cref="GetMissingPartsAsync"/> answered them.</param>
    /// <returns>The comma-separated body, with a trailing newline.</returns>
    /// <remarks>
    /// The path column names the folder and the file the part's picture belongs at, with no extension: the
    /// operator chooses the extension, and the application draws the ones the acceptance rule allows. Quoting is
    /// applied to a value that holds a comma, a quote or a newline, so a part number that would otherwise split a
    /// line stays one field.
    /// </remarks>
    public static string BuildExportCsv(IReadOnlyList<PartPictureMissRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var builder = new StringBuilder();
        builder.AppendLine(ExportHeader());

        foreach (var row in rows)
        {
            builder
                .Append(Escape(row.PartNumber)).Append(',')
                .Append(Escape(row.SystemName)).Append(',')
                .Append(Escape(row.FamilyFolder)).Append(',')
                .Append(Escape(row.RelativePath))
                .AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>The system in the words a person reads on the screen.</summary>
    /// <param name="system">The part's system.</param>
    /// <returns><c>Visual</c> or <c>WIP</c>.</returns>
    public static string SystemNameFor(PartPictureSystem system) => system switch
    {
        PartPictureSystem.Visual => "Visual",
        PartPictureSystem.Wip => "WIP",
        _ => system.ToString(),
    };

    private static string RelativeFolderAndNameFor(string scope, string partNumber) =>
        $"{PartPictureLayout.CollectionFolderFor(scope)}/{PartPictureLayout.FamilyFolderFor(partNumber)}/{PartPictureLayout.SafeFileBaseName(partNumber)}";

    /// <summary>
    /// The export's first line, from the localised resource so the file a person opens names its own columns.
    /// </summary>
    /// <returns>The header line.</returns>
    private static string ExportHeader()
    {
        var localized = "Settings_PartPictures_Missing_CsvHeader.Text".GetLocalized();

        // The resource lookup answers with the key itself when the resources are not loaded, which happens in a
        // test host. A file whose first line is a resource key is worse than a plainly spelled header.
        return string.IsNullOrWhiteSpace(localized)
            || string.Equals(localized, "Settings_PartPictures_Missing_CsvHeader.Text", StringComparison.Ordinal)
                ? "Part number,System,Family folder,Path the picture would be stored at"
                : localized;
    }

    private static string Escape(string value)
    {
        var text = value ?? string.Empty;

        return text.IndexOfAny([',', '"', '\n', '\r']) < 0
            ? text
            : $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
