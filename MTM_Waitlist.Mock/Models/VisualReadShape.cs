namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// The definition of one Infor Visual read shape — the unit of extensibility for the mirror cache.
/// </summary>
/// <remarks>
/// <para>
/// A shape is described once here and is consumed by both the in-app fallback library and the
/// on-host service's refresh catalog. Adding a shape must not change any existing shape's
/// artifact, contract, or result type (FR-016, FR-020).
/// </para>
/// <para>
/// <see cref="MirrorTableName"/>, <see cref="StageTableName"/>, <see cref="GetProcedureName"/>, and
/// <see cref="RefreshProcedureName"/> are <b>derived</b> from <see cref="Key"/>. A shape whose
/// artifacts do not exist under those derived names is invalid and is reported at service startup
/// rather than silently ignored.
/// </para>
/// </remarks>
public sealed record VisualReadShape
{
    /// <summary>Stable snake_case shape identifier, for example <c>work_order_lookup</c>.</summary>
    public required string Key { get; init; }

    /// <summary>Source-query module that owns the read script.</summary>
    public required VisualReadShapeModule Module { get; init; }

    /// <summary>
    /// Repository-relative path of the parameterized Visual query, for example
    /// <c>Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql</c>.
    /// </summary>
    /// <remarks>
    /// This is the per-key read the <b>application</b> attempts live before falling back to the
    /// mirror. It is not what the service refreshes from — see
    /// <see cref="PopulationScriptRelativePath"/>.
    /// </remarks>
    public required string SourceScriptRelativePath { get; init; }

    /// <summary>
    /// Repository-relative path of the service's set-based population read, for example
    /// <c>Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mirror is a <b>complete snapshot</b> of the shape's driver population, so the service reads
    /// it in one set-based statement per shape instead of one round trip per driver key. The script
    /// must project exactly <see cref="InputParameters"/> + <see cref="OutputColumns"/> under those
    /// names, because the payload it returns is handed straight to
    /// <c>sp_visual_&lt;shape&gt;_refresh</c> as JSON.
    /// </para>
    /// <para>
    /// <see langword="null"/> means "this shape has no population read and the service must not refresh
    /// it". Mirrors <see cref="IsEnabled"/>: both affect external refresh only and never gate an
    /// internal read (FR-001).
    /// </para>
    /// </remarks>
    public string? PopulationScriptRelativePath { get; init; }

    /// <summary>Ordered input parameters; must match the source script's parameters.</summary>
    public IReadOnlyList<VisualShapeParameter> InputParameters { get; init; } = [];

    /// <summary>
    /// Ordered output columns. Must match the live read's projection exactly, or the shape's
    /// refresh is marked failed and the last good snapshot is retained.
    /// </summary>
    public IReadOnlyList<VisualShapeColumn> OutputColumns { get; init; } = [];

    /// <summary>
    /// Per-shape refresh interval. <see langword="null"/> means "use the service's global default".
    /// </summary>
    public TimeSpan? RefreshIntervalOverride { get; init; }

    /// <summary>
    /// Lets an operator park a shape without deleting its artifacts. Affects external refresh and
    /// visibility only — it never gates an internal read or write (FR-001).
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Derived mirror table name: <c>visual_&lt;key&gt;_result</c>.</summary>
    public string MirrorTableName => $"visual_{Key}_result";

    /// <summary>Derived stage twin name: <c>visual_&lt;key&gt;_result_stage</c>.</summary>
    public string StageTableName => $"visual_{Key}_result_stage";

    /// <summary>Derived read procedure name: <c>sp_visual_&lt;key&gt;_get</c>.</summary>
    public string GetProcedureName => $"sp_visual_{Key}_get";

    /// <summary>Derived refresh procedure name: <c>sp_visual_&lt;key&gt;_refresh</c>.</summary>
    public string RefreshProcedureName => $"sp_visual_{Key}_refresh";
}
