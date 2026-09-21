using System.Text.Json.Serialization;

namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// The wire shapes of the service's HTTP API (<c>contracts/mock-service-http-api.md</c> §2/§3/§4/§6).
/// </summary>
/// <remarks>
/// These are the only types the network surface exposes. A caller is identified by the user name it presents
/// (T147); no payload here carries a credential, a connection string, or a password (SC-010).
/// </remarks>
public static class ServiceApiContracts
{
    /// <summary>`GET /api/status` response.</summary>
    /// <param name="OperatorAccessPermission">
    /// The permission that decides whether a caller may use this API, named so an operator can see what gates
    /// the service without reading its source (T147, T038). The roles that permission admits are stored data in
    /// the application store, so the service reports the permission rather than a copy of who holds it.
    /// </param>
    /// <param name="RefreshEnabled">
    /// Whether refresh runs on this host. <see langword="false"/> means Infor Visual is not reachable from
    /// here, so the scheduled loop and <c>POST /api/refresh</c> are disabled rather than failing repeatedly.
    /// </param>
    /// <param name="RefreshDisabledReason">Why refresh is disabled, or <see langword="null"/> when it is enabled.</param>
    public sealed record ServiceStatusPayload(
        string ServiceVersion,
        DateTimeOffset StartedUtc,
        bool VisualSourceReachable,
        bool RefreshEnabled,
        string? RefreshDisabledReason,
        string OperatorAccessPermission,
        int RefreshIntervalMinutes,
        IReadOnlyList<ShapeStatusPayload> Shapes,
        IReadOnlyList<BackupStatusPayload> Backups,
        AutoStartStatusPayload? AutoStart);

    /// <summary>Per-shape last-run and freshness status.</summary>
    /// <param name="ValidationError">
    /// Why a shape is excluded, when it failed startup validation. This is how a half-added shape is
    /// reported instead of silently never refreshing (FR-020).
    /// </param>
    public sealed record ShapeStatusPayload(
        string ShapeKey,
        bool IsEnabled,
        DateTime? LastRunUtc,
        string? LastOutcome,
        int? LastRowCount,
        DateTime? RefreshedUtc,
        bool IsSeedContentOnly,
        string? ValidationError);

    /// <summary>Per-store last-run and artifact status.</summary>
    /// <param name="IsStoreReachable">
    /// Whether this machine can reach the store's database. <see langword="false"/> means that store's backup
    /// and restore are disabled here, which is what a host that does not hold that database should report.
    /// </param>
    /// <param name="UnreachableReason">Why the store is unreachable, or <see langword="null"/> when it is.</param>
    public sealed record BackupStatusPayload(
        string Store,
        bool IsEnabled,
        bool IsStoreReachable,
        string? UnreachableReason,
        DateTime? LastRunUtc,
        string? LastOutcome,
        string? LastArtifactPath,
        int ArtifactCount,
        bool ToolAvailable);

    /// <summary>Auto-start reconciliation state (FR-007).</summary>
    public sealed record AutoStartStatusPayload(
        bool SettingEnabled,
        bool IsRegistered,
        bool IsReconciled,
        string Message);

    /// <summary>`POST /api/refresh` response: outcomes only, never partial per-shape data.</summary>
    public sealed record RefreshResponsePayload(
        string RunId,
        DateTimeOffset StartedUtc,
        DateTimeOffset FinishedUtc,
        IReadOnlyList<RefreshShapeOutcomePayload> Results);

    /// <summary>One shape's outcome within a refresh cycle.</summary>
    public sealed record RefreshShapeOutcomePayload(
        string ShapeKey,
        string Outcome,
        int? RowCount,
        long DurationMs,
        string? ErrorMessage);

    /// <summary>`POST /api/backup` response.</summary>
    public sealed record BackupResponsePayload(
        string Store,
        string Outcome,
        ArtifactPayload? Artifact);

    /// <summary>`GET /api/backups` response.</summary>
    public sealed record BackupListPayload(IReadOnlyList<ArtifactPayload> Artifacts);

    /// <summary>One backup artifact, without any secret material.</summary>
    public sealed record ArtifactPayload(
        Guid ArtifactId,
        string Store,
        DateTime CreatedUtc,
        string FilePath,
        long SizeBytes,
        bool IsRetained,
        bool IsSafetySnapshot);

    /// <summary>The error model (`contracts/mock-service-http-api.md` §6).</summary>
    /// <remarks>
    /// <see cref="Message"/> is omitted when there is no detail to give: the refusal path has none, and the
    /// contract writes that body as an error code on its own. The wire names are camelCase because every route
    /// serializes this record with web defaults — see <c>ServiceOperatorAuthenticationHandler</c>.
    /// </remarks>
    public sealed record ApiErrorPayload(
        string Error,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message);
}
