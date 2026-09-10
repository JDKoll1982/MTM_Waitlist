using System.Text;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Holds the read-shape catalog and validates it against the real artifacts at startup.
/// </summary>
/// <remarks>
/// <para>
/// Startup validation (FR-020, constitution III, <c>contracts/mock-service-configuration.md</c> §3)
/// checks, for every catalog shape:
/// </para>
/// <list type="number">
///   <item><description>the source Visual script exists on disk;</description></item>
///   <item><description>the mirror table, stage twin, and both procedures exist in <c>mtm_mock</c>;</description></item>
///   <item><description>the mirror's actual physical columns match the columns the catalog implies.</description></item>
/// </list>
/// <para>
/// Artifact existence is read through <c>sp_visual_read_shape_metadata_get</c>
/// (<see cref="IVisualShapeMetadataReader"/>) — never by inline SQL — so the SP-first rule holds
/// with no audit exemption.
/// </para>
/// <para>
/// <b>A shape that fails validation is excluded and reported; it never crashes the service.</b> One
/// bad shape must not take the whole service down, and the remaining shapes must still refresh
/// (FR-020). This is also how a half-added shape is surfaced: a shape with tables and procedures but
/// no catalog entry is never refreshed, and a catalog entry with missing artifacts is reported here.
/// </para>
/// </remarks>
public sealed class RefreshShapeCatalogProvider
{
    private readonly IVisualShapeMetadataReader _metadataReader;
    private readonly IReadOnlyList<VisualReadShape> _catalog;
    private readonly string _contentRoot;

    private IReadOnlyList<RefreshShapeCatalogEntry> _entries = [];

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="metadataReader">Reads artifact metadata from the <c>mtm_mock</c> database.</param>
    /// <param name="catalog">Shape definitions; defaults to <see cref="VisualReadShapeCatalog.Create"/>.</param>
    /// <param name="contentRoot">
    /// Root the source-script paths are resolved against. Defaults to the service's base directory,
    /// which is where the runnable app ships its <c>Database/InforVisual/Queues</c> content.
    /// </param>
    public RefreshShapeCatalogProvider(
        IVisualShapeMetadataReader metadataReader,
        IReadOnlyList<VisualReadShape>? catalog = null,
        string? contentRoot = null)
    {
        ArgumentNullException.ThrowIfNull(metadataReader);

        _metadataReader = metadataReader;
        _catalog = catalog ?? VisualReadShapeCatalog.Create();
        _contentRoot = contentRoot ?? AppContext.BaseDirectory;
    }

    /// <summary>Every catalog shape with its validation outcome. Empty until <see cref="ValidateAsync"/> runs.</summary>
    public IReadOnlyList<RefreshShapeCatalogEntry> Entries => _entries;

    /// <summary>Shapes that validated successfully and are enabled — the refresh engine's work list.</summary>
    public IReadOnlyList<VisualReadShape> RefreshableShapes =>
        _entries.Where(entry => entry.IsValid && entry.Shape.IsEnabled).Select(entry => entry.Shape).ToList();

    /// <summary>Shapes excluded from refresh because validation failed.</summary>
    public IReadOnlyList<RefreshShapeCatalogEntry> InvalidShapes =>
        _entries.Where(entry => !entry.IsValid).ToList();

    /// <summary>Shapes that are valid but deliberately parked by the operator.</summary>
    public IReadOnlyList<VisualReadShape> DisabledShapes =>
        _entries.Where(entry => entry.IsValid && !entry.Shape.IsEnabled).Select(entry => entry.Shape).ToList();

    /// <summary>
    /// Validates every catalog shape against the real artifacts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation outcomes, one per catalog shape.</returns>
    /// <remarks>
    /// This method never throws for a bad shape or an unreachable database: a failure to inspect a
    /// shape marks that shape invalid so it is skipped and reported while the others continue.
    /// </remarks>
    public async Task<IReadOnlyList<RefreshShapeCatalogEntry>> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<RefreshShapeCatalogEntry>(_catalog.Count);

        foreach (var shape in _catalog)
        {
            entries.Add(await ValidateShapeAsync(shape, cancellationToken).ConfigureAwait(false));
        }

        _entries = entries;
        return _entries;
    }

    private async Task<RefreshShapeCatalogEntry> ValidateShapeAsync(
        VisualReadShape shape,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(
            _contentRoot,
            shape.SourceScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(scriptPath))
        {
            return Invalid(shape, $"Source script not found: '{shape.SourceScriptRelativePath}'.");
        }

        VisualShapeMetadata? metadata;
        try
        {
            metadata = await _metadataReader
                .GetShapeMetadataAsync(shape.Key, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // An unreachable cache is reported as an invalid shape, never a crash (FR-020).
            return Invalid(shape, $"Could not read {shape.Key} artifact metadata: {exception.Message}");
        }

        if (metadata is null)
        {
            return Invalid(shape, $"No artifact metadata was returned for shape '{shape.Key}'.");
        }

        if (!metadata.HasAllArtifacts)
        {
            return Invalid(
                shape,
                $"Missing artifacts for '{shape.Key}': mirror={metadata.MirrorTableExists}, " +
                $"stage={metadata.StageTableExists}, get={metadata.GetProcedureExists}, " +
                $"refresh={metadata.RefreshProcedureExists}.");
        }

        var expectedColumns = BuildExpectedMirrorColumns(shape);
        var actualColumns = metadata.GetMirrorColumnList();

        if (!expectedColumns.SequenceEqual(actualColumns, StringComparer.OrdinalIgnoreCase))
        {
            return Invalid(
                shape,
                $"Mirror column mismatch for '{shape.Key}': expected [{string.Join(", ", expectedColumns)}], " +
                $"actual [{string.Join(", ", actualColumns)}].");
        }

        return new RefreshShapeCatalogEntry(shape, IsValid: true, InvalidReason: null);
    }

    private static RefreshShapeCatalogEntry Invalid(VisualReadShape shape, string reason) =>
        new(shape, IsValid: false, InvalidReason: reason);

    /// <summary>
    /// Derives the mirror's full physical column list from the shape definition.
    /// </summary>
    /// <remarks>
    /// The mirror's physical layout is always: <c>id</c>, then the input key columns, then the output
    /// columns, then <c>refreshed_utc</c> and <c>is_seed_content</c>. A column that is both an input
    /// and an output (shape 4's <c>part_number</c>) appears once, at its first position — matching
    /// <c>data-model.md</c> §3.
    /// </remarks>
    private static IReadOnlyList<string> BuildExpectedMirrorColumns(VisualReadShape shape)
    {
        var columns = new List<string> { "id" };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id" };

        void Add(string name)
        {
            var column = ToSnakeCase(name);
            if (seen.Add(column))
            {
                columns.Add(column);
            }
        }

        foreach (var parameter in shape.InputParameters)
        {
            Add(parameter.Name);
        }

        foreach (var column in shape.OutputColumns)
        {
            Add(column.Name);
        }

        columns.Add("refreshed_utc");
        columns.Add("is_seed_content");

        return columns;
    }

    /// <summary>
    /// Converts a PascalCase projection name to its mirror column name (<c>PartNumber</c> to
    /// <c>part_number</c>). Trailing digits do not introduce a separator, so <c>User8</c> becomes
    /// <c>user8</c> exactly as the mirror declares it.
    /// </summary>
    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsUpper(character))
            {
                if (index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
