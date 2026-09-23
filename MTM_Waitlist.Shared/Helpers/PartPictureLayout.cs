namespace MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// The one place the part-picture share layout is spelled: the three collections, the four family folders, and the
/// rule that turns a part number into a file name.
/// </summary>
/// <remarks>
/// <para>
/// The folder names here are the contract, not an implementation detail. A file name is how a person finds a
/// picture by hand on the share, and the bulk file-drop path depends on the name being the name they already
/// know, so nothing here renames, re-cases, pluralises or paraphrases one. Every string below is copied from the
/// feature's own layout contract.
/// </para>
/// <para>
/// The layout inside the configured root is compiled on purpose while the root itself stays a setting. What makes
/// a recorded value portable across machines is that it is stored relative to the root, not that these folders
/// are configurable.
/// </para>
/// </remarks>
public static class PartPictureLayout
{
    /// <summary>The collection folder holding everything the application pictures for its own screens.</summary>
    public const string WaitlistCollection = "Waitlist";

    /// <summary>The collection folder holding the Infor Visual parts.</summary>
    public const string VisualCollection = "Visual";

    /// <summary>The collection folder holding the WIP floor parts.</summary>
    public const string WipCollection = "WIP";

    /// <summary>The family folder for part numbers that begin <c>MMC</c> (coil).</summary>
    public const string CoilFolder = "MMC";

    /// <summary>The family folder for part numbers that begin <c>MMF</c> (flatstock).</summary>
    public const string FlatstockFolder = "MMF";

    /// <summary>The family folder for part numbers that begin <c>FGT</c> (die).</summary>
    public const string DieFolder = "FGT";

    /// <summary>The folder every part number no prefix recognises is kept in.</summary>
    public const string CatchAllFolder = "Categorized Parts";

    /// <summary>
    /// The longest part number this feature accepts, which is the width of the store column that has to hold it.
    /// A longer number is refused when its picture is set rather than stored truncated.
    /// </summary>
    public const int MaxPartNumberLength = 190;

    /// <summary>The store scope for an Infor Visual part picture.</summary>
    public const string VisualPartScope = "visual_part";

    /// <summary>The store scope for a WIP floor part picture.</summary>
    public const string WipPartScope = "wip_part";

    /// <summary>
    /// The two part scopes, in the order a screen lists them. A part picture is keyed by the system and the part
    /// number together, never by the part number alone, which is why this list exists rather than a single scope.
    /// </summary>
    public static readonly IReadOnlyList<string> PartScopes = [VisualPartScope, WipPartScope];

    /// <summary>
    /// The two collection folders that hold part pictures, in the order the scopes are listed.
    /// </summary>
    /// <remarks>
    /// These are the folders a walk of the share must leave alone, because what lives in them belongs to this
    /// machine's own copy store and is fetched a part at a time rather than mirrored in one pass (FR-030).
    /// </remarks>
    public static readonly IReadOnlyList<string> PartCollectionFolders = [VisualCollection, WipCollection];

    /// <summary>
    /// The mapping from a part number's prefix to its family, in the order the prefixes are tested. The prefix is
    /// authoritative: a part whose stored category disagrees with its number is still placed by its number.
    /// </summary>
    private static readonly IReadOnlyList<(string Prefix, string Family, string Folder)> PrefixFamilies =
    [
        ("MMC", "Coil", CoilFolder),
        ("MMF", "Flatstock", FlatstockFolder),
        ("FGT", "Die", DieFolder),
    ];

    /// <summary>Whether a scope is one of the two part scopes.</summary>
    /// <param name="scope">A store scope value.</param>
    /// <returns><see langword="true"/> when the scope holds a part picture.</returns>
    public static bool IsPartScope(string? scope) =>
        !string.IsNullOrWhiteSpace(scope)
        && PartScopes.Contains(scope.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The collection folder a scope's pictures live under.
    /// </summary>
    /// <param name="scope">A store scope value.</param>
    /// <returns>The collection folder name, or <see langword="null"/> for a scope this layout does not know.</returns>
    public static string? CollectionFolderFor(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        var normalized = scope.Trim();

        if (string.Equals(normalized, VisualPartScope, StringComparison.OrdinalIgnoreCase))
        {
            return VisualCollection;
        }

        if (string.Equals(normalized, WipPartScope, StringComparison.OrdinalIgnoreCase))
        {
            return WipCollection;
        }

        // The three scopes the application already had keep their own names verbatim as the kind folders inside
        // the application's own collection. That is what settles the case-only collision between the item `other`
        // and the category `Other` by folder rather than by renaming either of them.
        return IsApplicationOwnScope(normalized) ? WaitlistCollection : null;
    }

    /// <summary>
    /// The kind folder inside the application's own collection for one of its three scopes, which is the scope
    /// name itself.
    /// </summary>
    /// <param name="scope">A store scope value.</param>
    /// <returns>The kind folder name, or <see langword="null"/> for a scope that is not one of the three.</returns>
    public static string? KindFolderFor(string? scope) =>
        string.IsNullOrWhiteSpace(scope) || IsApplicationOwnScope(scope) is false
            ? null
            : scope.Trim();

    /// <summary>
    /// The family a part number's prefix puts it in, compared case-insensitively so <c>mmc0001000</c> and
    /// <c>MMC0001000</c> are one family and one file name.
    /// </summary>
    /// <param name="partNumber">The part number.</param>
    /// <returns><c>Coil</c>, <c>Flatstock</c> or <c>Die</c>, or <see langword="null"/> when no prefix matches.</returns>
    public static string? FamilyFor(string? partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
        {
            return null;
        }

        var trimmed = partNumber.Trim();

        foreach (var (prefix, family, _) in PrefixFamilies)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return family;
            }
        }

        return null;
    }

    /// <summary>
    /// The family folder a part number's picture is stored in and read from. A part no prefix recognises goes to
    /// the catch-all, so an unrecognised part is still picturable rather than silently unpicturable.
    /// </summary>
    /// <param name="partNumber">The part number.</param>
    /// <returns>The family folder name.</returns>
    public static string FamilyFolderFor(string? partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
        {
            return CatchAllFolder;
        }

        var trimmed = partNumber.Trim();

        foreach (var (prefix, _, folder) in PrefixFamilies)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return folder;
            }
        }

        return CatchAllFolder;
    }

    /// <summary>
    /// The part number as a file name: every character a file name cannot hold is replaced with an underscore.
    /// </summary>
    /// <param name="partNumber">The part number.</param>
    /// <returns>A base name that can be written to the share.</returns>
    /// <remarks>
    /// This is the rule the storage service already applies to an item id, stated once here so the writer and the
    /// reader cannot disagree about which file a part owns.
    /// </remarks>
    public static string SafeFileBaseName(string? partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
        {
            return string.Empty;
        }

        var trimmed = partNumber.Trim();
        var invalid = Path.GetInvalidFileNameChars();

        return trimmed.IndexOfAny(invalid) < 0 ? trimmed : new string(trimmed.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>
    /// The path a part's picture is stored at, relative to the configured picture root.
    /// </summary>
    /// <param name="scope">The part's scope: <c>visual_part</c> or <c>wip_part</c>.</param>
    /// <param name="partNumber">The part number.</param>
    /// <param name="extension">The picture's own extension, including the dot.</param>
    /// <returns>
    /// The relative path, for example <c>Visual/MMC/MMC0001000.png</c>, or <see langword="null"/> when the scope
    /// is not a part scope.
    /// </returns>
    public static string? RelativePathFor(string? scope, string? partNumber, string extension)
    {
        var collection = CollectionFolderFor(scope);
        if (collection is null || string.IsNullOrWhiteSpace(partNumber))
        {
            return null;
        }

        var folder = FamilyFolderFor(partNumber);
        var fileName = SafeFileBaseName(partNumber) + extension;

        return $"{collection}/{folder}/{fileName}";
    }

    private static bool IsApplicationOwnScope(string scope) =>
        string.Equals(scope, "request_item", StringComparison.OrdinalIgnoreCase)
        || string.Equals(scope, "request_category", StringComparison.OrdinalIgnoreCase)
        || string.Equals(scope, "work_center", StringComparison.OrdinalIgnoreCase);
}
