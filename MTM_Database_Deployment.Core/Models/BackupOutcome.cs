namespace MTM_Database_Deployment.Models;

/// <summary>
/// What a pre-deployment copy managed to keep.
/// </summary>
/// <remarks>
/// This is a summary rather than a pass/fail result: a database that does not exist yet, or that holds no
/// tables, has nothing to copy, and that is an ordinary outcome rather than a failure.
/// </remarks>
public sealed class BackupOutcome
{
    public string SourceDatabase { get; init; } = string.Empty;

    public string BackupDatabase { get; init; } = string.Empty;

    public int TableCount { get; set; }

    public long RowCount { get; set; }

    /// <summary>Views carried by a duplicate. A deployment's own pre-run copy carries none.</summary>
    public int ViewCount { get; set; }

    /// <summary>Stored procedures and functions carried by a duplicate. A pre-run copy carries none.</summary>
    public int RoutineCount { get; set; }

    /// <summary>True when a backup database was created; false when there was nothing to copy.</summary>
    public bool WasCreated { get; set; }

    public string Summary { get; set; } = string.Empty;
}
