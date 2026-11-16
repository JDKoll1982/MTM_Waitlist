namespace MTM_Database_Deployment.Models;

/// <summary>
/// Immutable result of a preview (What-If) run.
/// </summary>
public sealed class Model_DeploymentPlan
{
    public required string Server { get; init; }
    public required int Port { get; init; }
    public required string Database { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<Model_SchemaChange> Changes { get; init; } = Array.Empty<Model_SchemaChange>();

    public IEnumerable<Model_SchemaChange> Destructive => Changes.Where(change => change.RequiresConfirmation);

    public IEnumerable<Model_SchemaChange> Additive => Changes.Where(change => !change.RequiresConfirmation);

    public int CreateTableCount => Changes.Count(change => change.Kind == Enum_ChangeKind.CreateTable);
    public int AddColumnCount => Changes.Count(change => change.Kind == Enum_ChangeKind.AddColumn);
    public int AlterColumnCount => Changes.Count(change => change.Kind == Enum_ChangeKind.AlterColumn);
    public int IndexCount => Changes.Count(change => change.Kind == Enum_ChangeKind.AddIndex);
    public int ForeignKeyCount => Changes.Count(change => change.Kind == Enum_ChangeKind.AddForeignKey);
    public int ObjectUpdateCount => Changes.Count(change => change.Kind is Enum_ChangeKind.UpdateView
        or Enum_ChangeKind.UpdateProcedure or Enum_ChangeKind.UpdateTrigger or Enum_ChangeKind.RunSeed);
    public int ExtraCount => Changes.Count(change => change.IsRemoval);
    public int BlockingExtraCount => Changes.Count(change => change.IsBlocking);
    public bool HasWork => Changes.Count > 0;

    public string Summary =>
        $"tables +{CreateTableCount}, columns +{AddColumnCount}, columns ~{AlterColumnCount}, " +
        $"indexes +{IndexCount}, foreign keys +{ForeignKeyCount}, objects {ObjectUpdateCount}, extras {ExtraCount}" +
        (BlockingExtraCount > 0
            ? $" ({BlockingExtraCount} database-only column(s) block inserts until removed)"
            : string.Empty);

    public string ToReportText()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"Deployment preview - {Database} on {Server}:{Port}");
        builder.AppendLine($"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine(Summary);
        builder.AppendLine(new string('-', 100));

        foreach (var group in Changes.GroupBy(change => change.KindLabel).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.AppendLine($"[{group.Key}] ({group.Count()})");
            foreach (var change in group.OrderBy(change => change.Target, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  - {change.Target}{(string.IsNullOrWhiteSpace(change.Detail) ? string.Empty : " :: " + change.Detail)}");
            }
        }

        return builder.ToString();
    }
}
