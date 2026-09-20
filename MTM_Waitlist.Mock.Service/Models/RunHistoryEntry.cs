using System.Text.Json.Serialization;

namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One line of the service's run history — a single refresh or backup outcome, in the vocabulary the
/// reliability criteria are written in.
/// </summary>
/// <remarks>
/// <para>
/// The file this serializes into is read by <c>tools/measure-reliability-window.ps1</c> (T231), which is the
/// only reader that matters: the property names below are that tool's contract, and changing one without
/// changing the tool silently turns a measured criterion back into an unmeasured one. The reader asserts it
/// found records, so a rename shows up as a failure rather than as a perfect score.
/// </para>
/// <para>
/// A refresh entry and a backup entry share this type rather than splitting into two, because they share one
/// append-only file and one retention schedule. <see cref="Record"/> says which it is; the members that do not
/// apply to it are omitted from the JSON entirely (see <see cref="JsonIgnoreCondition.WhenWritingNull"/>).
/// </para>
/// </remarks>
public sealed record RunHistoryEntry
{
    /// <summary>Either <see cref="RunHistoryStore.RefreshRecordKind"/> or <see cref="RunHistoryStore.BackupRecordKind"/>.</summary>
    [JsonPropertyName("record")]
    public required string Record { get; init; }

    /// <summary>
    /// The scheduled cycle this refresh belonged to. Every shape refreshed in one cycle carries the same value,
    /// which is what lets a reader count <i>cycles</i> and not merely shapes — a cycle that refreshed four of
    /// five shapes has to be countable as the partial failure it is.
    /// </summary>
    [JsonPropertyName("cycleUtc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CycleUtc { get; init; }

    /// <summary>The shape key, for example <c>work_order_lookup</c>. Refresh entries only.</summary>
    [JsonPropertyName("shape")]
    public string? Shape { get; init; }

    /// <summary>
    /// The backup window this run belonged to, from the run's own start instant. Backup entries only.
    /// </summary>
    [JsonPropertyName("windowUtc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime WindowUtc { get; init; }

    /// <summary>The MySQL database name, for example <c>mtm_mock</c>. Backup entries only.</summary>
    [JsonPropertyName("store")]
    public string? Store { get; init; }

    /// <summary>
    /// The reading the criteria use: <see cref="RunHistoryStore.SucceededOutcome"/>,
    /// <see cref="RunHistoryStore.SkippedOutcome"/> or <see cref="RunHistoryStore.FailedOutcome"/>.
    /// </summary>
    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    /// <summary>
    /// Why the run did not succeed, as a short token: <c>sourceUnreachable</c> for a skip, or the failure
    /// class. Absent on success.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// The sanitized error text, when there is one. It is carried so the history explains itself — an outcome
    /// of <c>Failed</c> with no detail is a fact nobody can act on. Credential-shaped text is redacted before
    /// it is written, by the same helper the run records use.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>Rows returned by a successful refresh. Refresh entries only.</summary>
    [JsonPropertyName("rows")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rows { get; init; }

    /// <summary>
    /// What caused the run: <c>scheduled</c> or <c>onDemand</c>. Refresh entries only. A reader measuring
    /// SC-007 keeps <c>scheduled</c> and discards the rest.
    /// </summary>
    [JsonPropertyName("trigger")]
    public string? Trigger { get; init; }

    /// <summary>The produced artifact's full path. Backup entries only; absent when none was produced.</summary>
    [JsonPropertyName("artifactPath")]
    public string? ArtifactPath { get; init; }

    /// <summary>The produced artifact's size. Backup entries only; absent when none was produced.</summary>
    [JsonPropertyName("artifactBytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ArtifactBytes { get; init; }
}
