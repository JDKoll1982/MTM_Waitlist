namespace MTM_Database_Deployment.Models;

/// <summary>
/// The kinds of work the deployment engine can report before it writes anything.
/// </summary>
public enum Enum_ChangeKind
{
    CreateTable = 0,
    AddColumn = 1,
    AlterColumn = 2,
    AddIndex = 3,
    AddForeignKey = 4,
    UpdateView = 5,
    UpdateProcedure = 6,
    UpdateTrigger = 7,
    RunSeed = 8,
    ExtraTable = 9,
    ExtraColumn = 10,
    ExtraIndex = 11,
    ExtraObject = 12,
    RebuildTable = 13
}

/// <summary>
/// A single planned change, produced by the comparer and consumed by the applier.
/// </summary>
public sealed class Model_SchemaChange
{
    public required Enum_ChangeKind Kind { get; init; }
    public required string Target { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string? Sql { get; init; }
    public string? SourcePath { get; init; }

    /// <summary>
    /// True for a database-only column that is NOT NULL and has no default. Inserts that do not
    /// name it fail, which makes it block every seed and procedure that writes to the table.
    /// </summary>
    public bool IsBlocking { get; init; }

    public bool IsRemoval => Kind is Enum_ChangeKind.ExtraTable or Enum_ChangeKind.ExtraColumn
        or Enum_ChangeKind.ExtraIndex or Enum_ChangeKind.ExtraObject;

    public bool RequiresConfirmation => IsRemoval || Kind == Enum_ChangeKind.RebuildTable;

    public string KindLabel => Kind switch
    {
        Enum_ChangeKind.CreateTable => "Create table",
        Enum_ChangeKind.AddColumn => "Add column",
        Enum_ChangeKind.AlterColumn => "Alter column",
        Enum_ChangeKind.AddIndex => "Add index",
        Enum_ChangeKind.AddForeignKey => "Add foreign key",
        Enum_ChangeKind.UpdateView => "Update view",
        Enum_ChangeKind.UpdateProcedure => "Update procedure",
        Enum_ChangeKind.UpdateTrigger => "Update trigger",
        Enum_ChangeKind.RunSeed => "Run seed",
        Enum_ChangeKind.ExtraTable => "Extra table",
        Enum_ChangeKind.ExtraColumn => "Extra column",
        Enum_ChangeKind.ExtraIndex => "Extra index",
        Enum_ChangeKind.ExtraObject => "Extra object",
        Enum_ChangeKind.RebuildTable => "Rebuild table",
        _ => Kind.ToString()
    };

    public string DisplayText => string.IsNullOrWhiteSpace(Detail)
        ? $"{KindLabel}: {Target}"
        : $"{KindLabel}: {Target} - {Detail}";
}
