namespace MTM_Waitlist.Mock.Service.Api;

/// <summary>
/// The wire shapes of the service's HTTP API (<c>contracts/mock-service-http-api.md</c> §2/§3/§4/§6).
/// </summary>
/// <remarks>
/// These are the only types the network surface exposes. No payload here carries a credential, a
/// connection string, or a password (FR-026, SC-010).
/// </remarks>
public static class ServiceApiContracts
{
    /// <summary>`GET /api/status` response.</summary>
    public sealed record ServiceStatusPayload(
        string ServiceVersion,
        DateTimeOffset StartedUtc,
        bool VisualSourceReachable,
        bool CredentialConfigured,
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
    public sealed record BackupStatusPayload(
        string Store,
        bool IsEnabled,
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
    public sealed record ApiErrorPayload(string Error, string? Message);
}
