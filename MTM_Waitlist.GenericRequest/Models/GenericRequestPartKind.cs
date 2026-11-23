namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// Whether a stated part is a coil or a flatstock, decided by the plant's own part-number prefixes, and which Items
/// may state one.
/// </summary>
/// <remarks>
/// <para>
/// <c>MMC</c> is a coil and <c>MMF</c> is a flatstock, which is the same rule the plant's SQL queues use
/// (<c>PART_ID LIKE 'MMC%'</c> / <c>'MMF%'</c>) and the same rule the coil availability service uses when it reads
/// the coil off a job's subordinate-part payload. What a part <em>is</em> is read from its number, not from the item
/// the requester chose, because one item stands for both: the Pickup item is named "coil-or-flatstock" and delivers
/// either.
/// </para>
/// <para>
/// These two kinds are stocked as whole pieces and are not attributed to a work-order sequence, so the part step
/// states the part alone and the request records no sequence for it.
/// </para>
/// <para>
/// <b>Which Items may state one is the item's business.</b> A coil or flatstock item offers only these two kinds, and
/// every other item offers none of them, so a coil number typed against a die, component, finished-good or
/// work-in-process request is refused rather than answered with the wrong thing.
/// </para>
/// </remarks>
public static class GenericRequestPartKind
{
    /// <summary>The prefix Infor Visual gives a coil part number.</summary>
    public const string CoilPrefix = "MMC";

    /// <summary>The prefix Infor Visual gives a flatstock part number.</summary>
    public const string FlatstockPrefix = "MMF";

    /// <summary>
    /// The Items that ask for a coil or a flatstock, and therefore the only Items whose requester may state one.
    /// </summary>
    /// <remarks>
    /// The catalogue's names, read as stable ids: the Pickup item stands for both (<c>coil-or-flatstock</c>), and the
    /// Deliver category names the two separately. Every other Item — a die, a component, a finished good, work in
    /// process, outside service — is about something else, so a coil or flatstock part number typed there is a
    /// different thing from the one being asked for.
    /// </remarks>
    private static readonly string[] CoilOrFlatstockItemCodes =
    [
        "pickup-coil",
        "deliver-coil",
        "deliver-flatstock",
    ];

    /// <summary>Whether the given part number names a coil or a flatstock.</summary>
    public static bool IsCoilOrFlatstock(string? partNumber)
    {
        var part = (partNumber ?? string.Empty).Trim();
        return part.StartsWith(CoilPrefix, StringComparison.OrdinalIgnoreCase)
            || part.StartsWith(FlatstockPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the chosen Item asks for a coil or a flatstock.</summary>
    public static bool IsCoilOrFlatstockItem(string? itemCode)
    {
        var code = (itemCode ?? string.Empty).Trim();
        return CoilOrFlatstockItemCodes.Any(candidate => string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether the chosen Item's requester may state <paramref name="partNumber"/> at all: a coil or flatstock Item
    /// offers only coil and flatstock parts, and every other Item offers none of them.
    /// </summary>
    public static bool AllowsPart(string? itemCode, string? partNumber) =>
        IsCoilOrFlatstockItem(itemCode) == IsCoilOrFlatstock(partNumber);
}
