namespace MTM_Database_Deployment.Models;

/// <summary>
/// One database the operator can choose, with wording that says what it is for rather than only its
/// name. A non-technical operator should not have to know which schema holds the plant's real data.
/// </summary>
public sealed class Model_DatabaseOption
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }

    public string Display => $"{Title} - {Name}";

    /// <summary>
    /// Item controls fall back to the data item's text for their automation name, so one is provided.
    /// Without this a screen reader announces the type name instead of the database.
    /// </summary>
    public override string ToString() => Display;

    public static Model_DatabaseOption For(string databaseName)
    {
        var name = databaseName.Trim();
        var isTest = name.EndsWith("_test", StringComparison.OrdinalIgnoreCase);
        var isMirror = name.Equals("mtm_mock", StringComparison.OrdinalIgnoreCase);

        // A copy of the mirror is checked first: it is a test database by its ending, but what an operator needs to
        // know is that it is a copy of the cache, not a practice copy of the waitlist.
        if (isTest && name.StartsWith("mtm_mock", StringComparison.OrdinalIgnoreCase))
        {
            return new Model_DatabaseOption
            {
                Name = name,
                Title = "Cache mirror copy",
                Description = "A duplicate of the Infor Visual cache, for trying a rebuild without touching the one "
                    + "the app falls back to. Safe to change."
            };
        }

        if (isTest)
        {
            return new Model_DatabaseOption
            {
                Name = name,
                Title = "Test database",
                Description = "A practice copy. Nothing here is used by the app on the floor, so it is safe to try things."
            };
        }

        if (isMirror)
        {
            return new Model_DatabaseOption
            {
                Name = name,
                Title = "Cache mirror",
                Description = "The Infor Visual copy the app falls back to when the plant system is unreachable. "
                    + "Safe to rebuild; no operator data lives here."
            };
        }

        return new Model_DatabaseOption
        {
            Name = name,
            Title = "Live database",
            Description = "The database the app uses every day. Changes here are real."
        };
    }

    /// <summary>
    /// The configured databases in their configured order, then any the server holds that the settings file has
    /// never heard of.
    /// </summary>
    /// <remarks>
    /// The second half is what keeps a hand-made copy usable: an operator who duplicates a database with the tool
    /// has to be able to pick the copy on the next run without editing a settings file first.
    /// </remarks>
    public static IReadOnlyList<Model_DatabaseOption> Merge(
        IEnumerable<string> configured,
        IEnumerable<string> discovered)
    {
        var names = new List<string>();
        foreach (var name in configured.Concat(discovered))
        {
            var trimmed = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(trimmed);
            }
        }

        return names.Select(For).ToList();
    }
}
