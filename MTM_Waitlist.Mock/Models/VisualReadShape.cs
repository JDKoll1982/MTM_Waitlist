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
    public required string SourceScriptRelativePath { get; init; }

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
