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
    /// The folder holding the hopper reference pictures, inside the application's own collection
    /// (<c>Waitlist/hopper</c>).
    /// </summary>
    /// <remarks>
    /// The hopper pictures are keyed by the <b>size word</b> rather than by a part number, because what they
    /// picture is the size a person chose and not a part: <c>Small</c>, <c>Medium</c> and <c>Large</c>, one file
    /// each (D22, FR-077, FR-078). That is why they sit in a folder of their own instead of under one of the
    /// part-number family folders, and why the scope below is deliberately not one of the part scopes.
    /// The spelling matches <see cref="AppStoragePaths.HopperFolderDefault"/>, which is the folder the artwork
    /// already lives in on the share.
    /// </remarks>
    public const string HoppersFolder = "hopper";

    /// <summary>
    /// The file-name stem of each hopper picture, in the order the size choices are offered. The stem is the
    /// size's own word, which is also the answer the flow records and the key the card resolves, so one spelling
    /// serves the step, the share and the card.
    /// </summary>
    public static readonly IReadOnlyList<string> HopperSizeCodes = ["Small", "Medium", "Large"];

    /// <summary>The extension every hopper reference picture is expected to carry.</summary>
    public const string HopperPictureExtension = ".png";

    /// <summary>The store/read scope for a hopper size's reference picture.</summary>
    /// <remarks>
    /// A scope of its own rather than <see cref="VisualPartScope"/>: a size word is not a part number, and
    /// filing it under the Visual part scope would make the part-picture rules look for a part called "Large".
    /// </remarks>
    public const string HopperSizeScope = "hopper_size";

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

    /// <summary>
    /// The part-number prefixes the application knows, in the order they are tested: coil, flatstock and die.
    /// </summary>
    /// <remarks>
    /// Exposed as the one source of truth for "does this term start with a known prefix", which is the question
    /// the fuzzy incorrect-part search asks of the number the operator types before it zero-pads it (D24, FR-082).
    /// Derived from <see cref="PrefixFamilies"/> so a fourth family cannot be added here and forgotten there.
    /// </remarks>
    public static IReadOnlyList<string> KnownPartNumberPrefixes { get; } =
        PrefixFamilies.Select(family => family.Prefix).ToArray();

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

        // A hopper size's picture lives in the application's own collection, in the `hopper` folder. It is not a
        // part, so it is answered before the part-family rules below rather than by them.
        if (IsHopperSizeScope(normalized))
        {
            return WaitlistCollection;
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

        // A hopper size is filed by its size word in the `hopper` folder, never by a part-number family: there is
        // no part here to derive a family from, and "Large" must not be filed under the catch-all as if it were an
        // unrecognised part (D22, FR-078).
        if (IsHopperSizeScope(scope))
        {
            var hopperFileName = HopperPictureFileName(partNumber);

            return hopperFileName.Length == 0 ? null : $"{collection}/{HoppersFolder}/{hopperFileName}";
        }

        var folder = FamilyFolderFor(partNumber);
        var fileName = SafeFileBaseName(partNumber) + extension;

        return $"{collection}/{folder}/{fileName}";
    }

    /// <summary>
    /// Whether a scope is the hopper size pictures' own scope.
    /// </summary>
    /// <param name="scope">A store or read scope value.</param>
    /// <returns><see langword="true"/> for the hopper size scope.</returns>
    public static bool IsHopperSizeScope(string? scope) =>
        !string.IsNullOrWhiteSpace(scope)
        && string.Equals(scope.Trim(), HopperSizeScope, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a scope is one this layout can produce a picture path for: a part scope, or the hopper size scope.
    /// </summary>
    /// <param name="scope">A store or read scope value.</param>
    /// <returns><see langword="true"/> when the layout knows where the picture lives.</returns>
    public static bool IsKnownPictureScope(string? scope) =>
        IsPartScope(scope) || IsHopperSizeScope(scope);

    /// <summary>
    /// Whether a value is one of the three hopper sizes, compared case-insensitively so <c>large</c> and
    /// <c>Large</c> are one size and one file.
    /// </summary>
    /// <param name="size">The size word a flow recorded or a card drew.</param>
    /// <returns><see langword="true"/> for Small, Medium or Large.</returns>
    public static bool IsHopperSizeCode(string? size) =>
        !string.IsNullOrWhiteSpace(size)
        && HopperSizeCodes.Contains(size.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The file name a hopper size's reference picture is stored under: the size's own word, canonicalised to the
    /// spelling the layout lists.
    /// </summary>
    /// <param name="size">The size word.</param>
    /// <returns>The file name, for example <c>Large.png</c>.</returns>
    public static string HopperPictureFileName(string? size) =>
        CanonicalHopperSize(size) is { Length: > 0 } canonical
            ? canonical + HopperPictureExtension
            : string.Empty;

    /// <summary>
    /// The path a hopper size's reference picture is stored at, relative to the configured picture root.
    /// </summary>
    /// <param name="size">The size word.</param>
    /// <returns>
    /// The relative path, for example <c>Waitlist/hopper/Large.png</c>, or <see langword="null"/> when the value
    /// is not one of the three sizes.
    /// </returns>
    public static string? HopperPictureRelativePath(string? size)
    {
        var fileName = HopperPictureFileName(size);

        return fileName.Length == 0 ? null : $"{WaitlistCollection}/{HoppersFolder}/{fileName}";
    }

    /// <summary>The size word's canonical spelling, or empty when the value is not one of the three sizes.</summary>
    private static string CanonicalHopperSize(string? size) =>
        string.IsNullOrWhiteSpace(size)
            ? string.Empty
            : HopperSizeCodes.FirstOrDefault(code => string.Equals(code, size.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? string.Empty;

    private static bool IsApplicationOwnScope(string scope) =>
        string.Equals(scope, "request_item", StringComparison.OrdinalIgnoreCase)
        || string.Equals(scope, "request_category", StringComparison.OrdinalIgnoreCase)
        || string.Equals(scope, "work_center", StringComparison.OrdinalIgnoreCase);
}
