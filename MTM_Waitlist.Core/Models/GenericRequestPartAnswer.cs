namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The part answer a generic part request records on itself, and reads back from its own stored answers.
/// </summary>
/// <remarks>
/// <para>
/// These values ride on the request's existing <c>answers_json</c> column through the same insert and list
/// procedures every request already uses, so nothing in the schema changes. They live here, in Core, because both
/// the flow that writes them and the board that reads them already reference it.
/// </para>
/// <para>
/// The part, the sequence, the system that supplied the sequence, the figure and the system that answered it are
/// what the request is about (FR-016). A request that never reached a reading records only what it has, and a
/// missing value reads back as absent rather than as zero.
/// </para>
/// </remarks>
public sealed record GenericRequestPartAnswer
{
    /// <summary>The stated part number.</summary>
    public const string PartNumberKey = "part.number";

    /// <summary>The chosen sequence.</summary>
    public const string SequenceKey = "part.sequence";

    /// <summary>Which systems supplied the sequence, joined the way the list showed them.</summary>
    public const string SequenceSourceKey = "part.sequenceSource";

    /// <summary>The units on hand, or absent when nothing was read.</summary>
    public const string QuantityOnHandKey = "part.quantityOnHand";

    /// <summary>The system that answered the quantity.</summary>
    public const string AnsweredByKey = "part.answeredBy";

    /// <summary>The derived stage the operation state gave.</summary>
    public const string StageKey = "part.stage";

    public string PartNumber { get; init; } = string.Empty;

    public string Sequence { get; init; } = string.Empty;

    public string SequenceSource { get; init; } = string.Empty;

    /// <summary>The units on hand. Null when nothing was read.</summary>
    public decimal? QuantityOnHand { get; init; }

    /// <summary>The system that answered the quantity, by its own name, or empty when nothing answered.</summary>
    public string AnsweredBy { get; init; } = string.Empty;

    public string Stage { get; init; } = string.Empty;

    /// <summary>Whether this answer states a part at all.</summary>
    public bool HasPart => PartNumber.Length > 0;

    /// <summary>The answers a request records for this part answer, ready for <c>answers_json</c>.</summary>
    public Dictionary<string, string> ToAnswers()
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PartNumberKey] = PartNumber,
        };

        Add(answers, SequenceKey, Sequence);
        Add(answers, SequenceSourceKey, SequenceSource);
        Add(answers, AnsweredByKey, AnsweredBy);
        Add(answers, StageKey, Stage);

        if (QuantityOnHand is { } figure)
        {
            answers[QuantityOnHandKey] = figure.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return answers;
    }

    /// <summary>
    /// Read the part answer back from a request's stored answers, or null when the request states no part.
    /// </summary>
    public static GenericRequestPartAnswer? TryRead(IReadOnlyDictionary<string, string>? answers)
    {
        if (answers is null)
        {
            return null;
        }

        var partNumber = Read(answers, PartNumberKey).Trim();
        if (partNumber.Length == 0)
        {
            return null;
        }

        var figureText = Read(answers, QuantityOnHandKey);
        var hasFigure = decimal.TryParse(
            figureText,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var figure);

        return new GenericRequestPartAnswer
        {
            PartNumber = partNumber,
            Sequence = Read(answers, SequenceKey).Trim(),
            SequenceSource = Read(answers, SequenceSourceKey).Trim(),
            QuantityOnHand = hasFigure ? figure : null,
            AnsweredBy = Read(answers, AnsweredByKey).Trim(),
            Stage = Read(answers, StageKey).Trim(),
        };
    }

    private static void Add(Dictionary<string, string> answers, string key, string value)
    {
        if (value.Length > 0)
        {
            answers[key] = value;
        }
    }

    private static string Read(IReadOnlyDictionary<string, string> answers, string key) =>
        answers.TryGetValue(key, out var value) && value is not null ? value : string.Empty;
}
