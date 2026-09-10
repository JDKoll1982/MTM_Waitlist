namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One row of <c>sp_visual_read_shape_metadata_get</c>: which artifacts exist for a read shape,
/// plus the mirror table's actual physical column list.
/// </summary>
/// <param name="ShapeKey">The requested/derived shape key.</param>
/// <param name="MirrorTableExists">1 when <c>visual_&lt;key&gt;_result</c> exists.</param>
/// <param name="StageTableExists">1 when <c>visual_&lt;key&gt;_result_stage</c> exists.</param>
/// <param name="GetProcedureExists">1 when <c>sp_visual_&lt;key&gt;_get</c> exists.</param>
/// <param name="RefreshProcedureExists">1 when <c>sp_visual_&lt;key&gt;_refresh</c> exists.</param>
/// <param name="MirrorColumns">
/// Comma-joined ACTUAL physical column names of the mirror table; <see langword="null"/> when the
/// table is absent. This includes the metadata columns <c>id</c>, <c>refreshed_utc</c>, and
/// <c>is_seed_content</c>.
/// </param>
public sealed record VisualShapeMetadata(
    string ShapeKey,
    int MirrorTableExists,
    int StageTableExists,
    int GetProcedureExists,
    int RefreshProcedureExists,
    string? MirrorColumns)
{
    /// <summary>Whether every artifact the shape requires exists.</summary>
    public bool HasAllArtifacts =>
        MirrorTableExists == 1 && StageTableExists == 1 && GetProcedureExists == 1 && RefreshProcedureExists == 1;

    /// <summary>The mirror's physical columns, or an empty list when the table is absent.</summary>
    public IReadOnlyList<string> GetMirrorColumnList() =>
        string.IsNullOrWhiteSpace(MirrorColumns)
            ? []
            : MirrorColumns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
