using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Discovers every SQL object script that the deployment ships.
/// </summary>
/// <remarks>
/// The repository keeps one folder per object holding a <c>create.sql</c> and a matching <c>rollback.sql</c>, so the
/// catalog reads the create script and ignores the rest. Aggregates such as <c>AllSPs.sql</c> are ignored too: they
/// are a concatenation of the same objects, and applying both would run every script twice.
/// </remarks>
public sealed class Service_ObjectCatalog
{
    private static readonly (string Folder, Enum_SqlObjectKind Kind)[] s_folders =
    [
        ("Tables", Enum_SqlObjectKind.Schema),
        ("Views", Enum_SqlObjectKind.View),
        ("StoredProcedures", Enum_SqlObjectKind.Procedure),
        ("Functions", Enum_SqlObjectKind.Function),
        ("Triggers", Enum_SqlObjectKind.Trigger),
        ("Seeds", Enum_SqlObjectKind.Seed)
    ];

    private readonly string _sqlRoot;

    public Service_ObjectCatalog(string sqlRoot)
    {
        _sqlRoot = sqlRoot;
    }

    public string SqlRoot => _sqlRoot;

    /// <summary>
    /// The maintained description file, which sits beside the bootstrap rather than under a kind folder because it
    /// describes the whole store at once.
    /// </summary>
    public static string DescriptionsScriptPath(string sqlRoot) =>
        Path.Combine(sqlRoot, "Bootstrap", "update_table_descriptions.sql");

    public IReadOnlyList<Model_SqlObject> LoadObjects()
    {
        var objects = new List<Model_SqlObject>();
        if (!Directory.Exists(_sqlRoot))
        {
            return objects;
        }

        foreach (var (folder, kind) in s_folders)
        {
            objects.AddRange(LoadFolder(folder, kind));
        }

        return objects
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<Model_SqlObject> LoadFolder(string folderName, Enum_SqlObjectKind kind)
    {
        var folderPath = Path.Combine(_sqlRoot, folderName);
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        var archiveMarker = $"{Path.DirectorySeparatorChar}_Archive{Path.DirectorySeparatorChar}";
        var bootstrapMarker = $"{Path.DirectorySeparatorChar}Bootstrap{Path.DirectorySeparatorChar}";

        return Directory
            .EnumerateFiles(folderPath, "create.sql", SearchOption.AllDirectories)
            .Where(path => !path.Contains(archiveMarker, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(bootstrapMarker, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new Model_SqlObject
            {
                Name = Path.GetFileName(Path.GetDirectoryName(path)) ?? Path.GetFileNameWithoutExtension(path),
                FullPath = path,
                RelativePath = Path.GetRelativePath(_sqlRoot, path).Replace('\\', '/'),
                Kind = kind
            })
            // A development reset empties tables on purpose so a developer starts from a known state. It is never
            // part of bringing a real database up to date, so it is not deployed at all.
            .Where(item => !item.Name.StartsWith("seed_dev_", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
