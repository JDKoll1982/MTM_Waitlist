namespace MTM_Database_Deployment.Models;

/// <summary>
/// A plain-language group of planned changes. The operator sees these groups instead of the raw
/// change kinds, so a deployment reads as "12 new detail fields" rather than twelve ALTER statements.
/// </summary>
public sealed class Model_ChangeGroup
{
    public required string Title { get; init; }

    /// <summary>Segoe Fluent glyph shown beside the title.</summary>
    public required string Glyph { get; init; }

    public required string Summary { get; init; }

    public bool IsWarning { get; init; }

    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();

    public bool HasItems => Items.Count > 0;
}
