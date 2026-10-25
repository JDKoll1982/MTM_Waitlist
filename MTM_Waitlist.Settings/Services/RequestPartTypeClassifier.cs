using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Derives a <see cref="RequestPartType"/> from the plant's own Infor Visual classification of a part.
/// </summary>
/// <remarks>
/// <para>
/// <b>The codes classify; the description and the part-number prefix only break ties.</b> Measured on the live
/// install (VISUAL/MTMFG, 2026-10-23, 8,895 parts): 50 parts describe themselves with a fastener word but are
/// stamped parts ("Bolt Retainer Assembly"), and 73 parts coded <c>M Fast Fastener</c> never name a fastener
/// ("Boss", "Plug, Pipe", "Insulator"). Letting the description win would misfile both groups, so a description or
/// prefix is consulted only to decide what kind of <b>customer-supplied</b> material a part is — the one family whose
/// commodity code does not say (200 parts, of which 144 are coils and 34 flatstock).
/// </para>
/// <para>
/// The part-number prefixes the plant uses are <c>MMC</c> (coil), <c>MMF</c> (flatstock), <c>MMR</c> (round),
/// with <c>CS</c> (customer supplied) and <c>SR</c> (special request) suffixes — <c>MMCCS</c>, <c>MMCSR</c>,
/// <c>MMFCS</c>, <c>MMFSR</c>. They are never the primary rule: <c>MMC</c> also holds five flatstock parts and
/// <c>MMF</c> eight coil parts, and 45 tooling parts carry no <c>FGT</c> prefix at all.
/// </para>
/// </remarks>
public static class RequestPartTypeClassifier
{
    /// <summary>The codes and words the classifier reads for one part.</summary>
    /// <param name="ProductCode">Infor Visual <c>PART.PRODUCT_CODE</c>.</param>
    /// <param name="CommodityCode">Infor Visual <c>PART.COMMODITY_CODE</c>.</param>
    /// <param name="PartNumber">The part number, whose prefix breaks a customer-supplied tie.</param>
    /// <param name="Description">The part description, whose words break a customer-supplied tie.</param>
    public readonly record struct PartTypeInput(
        string? ProductCode,
        string? CommodityCode,
        string? PartNumber,
        string? Description);

    /// <summary>The customer-supplied commodity code, whose kind the other two columns have to name.</summary>
    private const string CustomerSuppliedCode = "M MATL CUST SUP";

    /// <summary>Classifies one part, answering <see cref="RequestPartType.Unknown"/> when nothing named a kind.</summary>
    public static RequestPartType Classify(PartTypeInput input)
    {
        var product = Normalize(input.ProductCode);
        var commodity = Normalize(input.CommodityCode);

        // Expense families are a product-code statement and outrank the commodity: EXP_PKG parts are bags, packs and
        // cartons whatever material they are made of.
        if (product.StartsWith("EXP_", StringComparison.Ordinal))
        {
            return RequestPartType.Expense;
        }

        switch (commodity)
        {
            case "T MFG USE TOOLS":
                return RequestPartType.Tooling;

            case "M DIE PUNCH":
            case "E TOOLING":
                return RequestPartType.DieComponent;

            case "E STAMPED ITEM":
                return RequestPartType.StampedPart;

            case "E ASSY ITEM":
                return RequestPartType.Assembly;

            case "E MACH ITEM":
                return RequestPartType.MachinedPart;

            case "M FAST FASTENER":
                return RequestPartType.Hardware;

            case "M MATL COIL":
            case "S MATL COIL":
                return RequestPartType.Coil;

            case "M MATL FLAT":
                return RequestPartType.Flatstock;

            case "M MATL ROUND":
                return RequestPartType.BarRound;

            case "M MATL NET-SHAP":
            case "S MATL NET-SHAP":
            case "M MATL MISC":
                return RequestPartType.RawOther;

            case CustomerSuppliedCode:
                return ClassifyCustomerSupplied(input);
        }

        // A tooling product code with no commodity kind still names tooling.
        if (product == "MFG_TOOLING")
        {
            return RequestPartType.Tooling;
        }

        return RequestPartType.Unknown;
    }

    /// <summary>
    /// What kind of customer-supplied material a part is: the commodity code says only that the customer supplied
    /// it, so the part number's prefix decides first (the plant's own <c>MMCCS</c> / <c>MMFCS</c> convention) and
    /// the description's first material word decides otherwise.
    /// </summary>
    private static RequestPartType ClassifyCustomerSupplied(PartTypeInput input)
    {
        var partNumber = Normalize(input.PartNumber);

        if (partNumber.StartsWith("MMC", StringComparison.Ordinal))
        {
            return RequestPartType.Coil;
        }

        if (partNumber.StartsWith("MMF", StringComparison.Ordinal))
        {
            return RequestPartType.Flatstock;
        }

        if (partNumber.StartsWith("MMR", StringComparison.Ordinal))
        {
            return RequestPartType.BarRound;
        }

        var description = Normalize(input.Description);

        if (ContainsWord(description, "COIL"))
        {
            return RequestPartType.Coil;
        }

        if (ContainsWord(description, "FLAT") || ContainsWord(description, "SHEET") || ContainsWord(description, "STRIP"))
        {
            return RequestPartType.Flatstock;
        }

        if (ContainsWord(description, "ROUND") || ContainsWord(description, "BAR") || ContainsWord(description, "ROD"))
        {
            return RequestPartType.BarRound;
        }

        return RequestPartType.CustomerSupplied;
    }

    /// <summary>The upper-cased, trimmed value, with runs of spaces collapsed so the comparisons are exact.</summary>
    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Whether a normalized description carries a word. The description's punctuation is turned into spaces first,
    /// so "Coil, .018 X 24.203" and "COIL-12" both answer yes and "COILER" does not.
    /// </summary>
    private static bool ContainsWord(string description, string word)
    {
        if (description.Length == 0)
        {
            return false;
        }

        var words = description.Split(
            [' ', ',', '.', '/', '\\', '-', '"', '\'', '(', ')', '#', '*', ':', ';', '+', '&'],
            StringSplitOptions.RemoveEmptyEntries);

        return words.Any(candidate => string.Equals(candidate, word, StringComparison.Ordinal));
    }
}
