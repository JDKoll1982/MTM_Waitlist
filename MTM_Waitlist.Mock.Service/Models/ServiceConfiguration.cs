using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The service's durable, service-local configuration (FR-012).
/// </summary>
/// <remarks>
/// <para>
/// Persisted under the service's own app-data folder — never a client-shared store — because the
/// service must stay configurable and reportable while external sources are unreachable, and
/// because a restore replaces an entire database (configuration living in a restored store would
/// be rewound by the operation it needs to describe).
/// </para>
/// <para>
/// A save is all-or-nothing: a partially written configuration is never loaded
/// (<c>contracts/mock-service-configuration.md</c> §1).
/// </para>
/// </remarks>
public sealed record ServiceConfiguration
{
    /// <summary>
    /// Global refresh interval. A shape may override it (see
    /// <see cref="VisualReadShape.RefreshIntervalOverride"/>).
    /// </summary>
    /// <remarks>
    /// The interval is a <b>grid anchored at local midnight</b>, not "every N minutes since the last
    /// run": with the shipped default of 3 hours the cache is refreshed at 00:00, 03:00, 06:00,
    /// 09:00, 12:00, 15:00, 18:00 and 21:00 <b>server-local</b> time. A long-running or skipped
    /// cycle therefore cannot drag the schedule off those times.
    /// </remarks>
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromHours(3);

    /// <summary>Read-only external Visual connection settings.</summary>
    public VisualSourceSettings VisualSource { get; init; } = new();

    /// <summary>
    /// MySQL host/login details for the <c>mtm_mock</c> cache and for the four stores' backups. The
    /// password is never part of this configuration; it comes from the environment and, for
    /// <c>mysqldump</c>, from an option file (FR-026).
    /// </summary>
    public MySqlConnectionSettings MySqlConnection { get; init; } = new();

    /// <summary>Network API bind address, port, and credential.</summary>
    public ApiSettings Api { get; init; } = new();

    /// <summary>
    /// Per-store backup policies. Every one of the four stores always has an entry — the set is
    /// fixed and an unknown key is a configuration error, not an extension point.
    /// </summary>
    public IReadOnlyDictionary<BackupStore, BackupPolicy> BackupPolicies { get; init; } =
        new Dictionary<BackupStore, BackupPolicy>();

    /// <summary>
    /// Whether the service registers its per-user logon auto-start entry. The <i>effect</i> is the
    /// presence or absence of the <c>HKCU\...\Run</c> value, which must be reconciled and reported
    /// at startup rather than silently ignored (FR-007).
    /// </summary>
    public bool AutoStartAtLogon { get; init; } = true;

    /// <summary>
    /// Explicit path to <c>mysqldump</c>. <see langword="null"/> means "resolve from <c>PATH</c>".
    /// Absence is reported as a distinct outcome, never worked around (FR-013).
    /// </summary>
    public string? MysqldumpPath { get; init; }

    /// <summary>
    /// Builds the shipped defaults, including one enabled backup policy per store.
    /// </summary>
    /// <param name="serviceAppDataRoot">Root folder for the service's own data (backups live beneath it).</param>
    /// <remarks>
    /// These are the <b>shipped defaults</b> that SC-007 and SC-008 are measured against
    /// (data-model.md §5/§6): refresh on the eight 3-hour slots anchored at local midnight
    /// (00:00/03:00/06:00/09:00/12:00/15:00/18:00/21:00 server-local); per-store backups at 01:00,
    /// 01:20, 01:40, and 02:00 local; 14 artifacts retained per store; API on <c>0.0.0.0:5760</c>.
    /// </remarks>
    public static ServiceConfiguration CreateDefault(string serviceAppDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceAppDataRoot);

        var backupRoot = Path.Combine(serviceAppDataRoot, "backups");

        var policies = new Dictionary<BackupStore, BackupPolicy>
        {
            [BackupStore.MtmWaitlist] = new()
            {
                Store = BackupStore.MtmWaitlist,
                ScheduleLocalTime = new TimeOnly(1, 0),
                DestinationDirectory = Path.Combine(backupRoot, BackupStore.MtmWaitlist.ToDatabaseName())
            },
            [BackupStore.MtmWipApplicationWinforms] = new()
            {
                Store = BackupStore.MtmWipApplicationWinforms,
                ScheduleLocalTime = new TimeOnly(1, 20),
                DestinationDirectory = Path.Combine(backupRoot, BackupStore.MtmWipApplicationWinforms.ToDatabaseName())
            },
            [BackupStore.MtmReceivingApplication] = new()
            {
                Store = BackupStore.MtmReceivingApplication,
                ScheduleLocalTime = new TimeOnly(1, 40),
                DestinationDirectory = Path.Combine(backupRoot, BackupStore.MtmReceivingApplication.ToDatabaseName())
            },
            [BackupStore.MtmMock] = new()
            {
                Store = BackupStore.MtmMock,
                ScheduleLocalTime = new TimeOnly(2, 0),
                DestinationDirectory = Path.Combine(backupRoot, BackupStore.MtmMock.ToDatabaseName())
            }
        };

        return new ServiceConfiguration { BackupPolicies = policies };
    }
}
