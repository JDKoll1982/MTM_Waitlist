namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The scrap type a generic requester stated, and reads back from the request's own stored answers.
/// </summary>
/// <remarks>
/// <para>
/// A generic work center has no job to read a scrap decision from, so the requester states the type and the request
/// carries it. The value rides on the request's existing <c>answers_json</c> column through the same insert and list
/// procedures every request already uses, so nothing in the schema changes (FR-005).
/// </para>
/// <para>
/// Only the type is kept. The lugger it dumps into is a live fact read from the arrangement whenever the card is
/// drawn, so a reassignment is followed and a blocked lugger is never named from a stored copy (FR-012, A2).
/// </para>
/// <para>
/// It mirrors <see cref="GenericRequestPartAnswer"/> and <see cref="GenericRequestDunnageAnswer"/> in the same
/// folder: const keys, a case-insensitive dictionary, and a read that answers nothing rather than throwing when a
/// request states no scrap type.
/// </para>
/// </remarks>
public sealed record GenericRequestScrapAnswer
{
    /// <summary>The scrap type the requester stated, in the wording they chose.</summary>
    public const string ScrapTypeKey = "scrap.type";

    /// <summary>The stated type's wording, or blank when the request states none.</summary>
    public string ScrapType { get; init; } = string.Empty;

    /// <summary>
    /// The answers a request records for this scrap answer, ready for <c>answers_json</c>.
    /// </summary>
    /// <remarks>
    /// A blank type writes no key at all, so a request that never reached the step records nothing about scrap
    /// rather than an empty answer that reads as a decision.
    /// </remarks>
    public Dictionary<string, string> ToAnswers()
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(ScrapType))
        {
            answers[ScrapTypeKey] = ScrapType.Trim();
        }

        return answers;
    }

    /// <summary>
    /// Read the scrap answer back from a request's stored answers.
    /// </summary>
    /// <param name="answers">The request's stored answers, or <c>null</c>.</param>
    /// <returns>The answer, or <c>null</c> when the request states no scrap type.</returns>
    /// <remarks>
    /// The wording is only trimmed, never rewritten, so a type later renamed or removed in the arrangement still
    /// reads as the requester stated it (FR-011).
    /// </remarks>
    public static GenericRequestScrapAnswer? TryRead(IReadOnlyDictionary<string, string>? answers)
    {
        if (answers is null)
        {
            return null;
        }

        var scrapType = Read(answers, ScrapTypeKey).Trim();
        if (scrapType.Length == 0)
        {
            return null;
        }

        return new GenericRequestScrapAnswer { ScrapType = scrapType };
    }

    private static string Read(IReadOnlyDictionary<string, string> answers, string key)
    {
        if (answers.TryGetValue(key, out var value) && value is not null)
        {
            return value;
        }

        // The dictionary a stored record is read from is built by whatever deserialised it, so its own comparer is
        // not something this record may rely on. The keys are compared case-insensitively by the contract, so the
        // lookup is completed here when the dictionary answers only its exact spelling.
        foreach (var candidate in answers)
        {
            if (string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase) && candidate.Value is not null)
            {
                return candidate.Value;
            }
        }

        return string.Empty;
    }
}
