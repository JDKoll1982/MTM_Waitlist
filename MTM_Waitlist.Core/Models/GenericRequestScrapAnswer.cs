namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The scrap type a generic request states on itself, and reads back from its own stored answers.
/// </summary>
/// <remarks>
/// <para>
/// A generic request has no job to read a scrap decision from, so the requester states the type and the request
/// carries it. It rides the request's existing <c>answers_json</c> column through the same insert and list
/// procedures every request already uses, so nothing in the schema changes.
/// </para>
/// <para>
/// It mirrors <see cref="GenericRequestDunnageAnswer"/> in the same folder: a const key, a case-insensitive
/// dictionary, and a read that answers nothing rather than throwing when a request states no scrap type.
/// </para>
/// </remarks>
public sealed record GenericRequestScrapAnswer(string ScrapType)
{
    /// <summary>The recorded key, fixed as <c>scrap.type</c>.</summary>
    public const string ScrapTypeKey = "scrap.type";

    /// <summary>The answers a request records for this scrap answer, ready for <c>answers_json</c>.</summary>
    public Dictionary<string, string> ToAnswers() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ScrapTypeKey] = ScrapType,
        };

    /// <summary>
    /// Read the stated scrap type back from a request's stored answers.
    /// </summary>
    /// <param name="answers">The request's stored answers, or <c>null</c>.</param>
    /// <returns>The answer, or <c>null</c> when the request states no scrap type.</returns>
    /// <remarks>
    /// A missing dictionary, a missing key and a stored value that is empty or whitespace all answer nothing. The
    /// card then has no stated type to resolve a lugger for, rather than a blank one to look up.
    /// </remarks>
    public static GenericRequestScrapAnswer? TryRead(IReadOnlyDictionary<string, string>? answers)
    {
        var scrapType = Read(answers, ScrapTypeKey).Trim();

        return scrapType.Length == 0 ? null : new GenericRequestScrapAnswer(scrapType);
    }

    private static string Read(IReadOnlyDictionary<string, string>? answers, string key)
    {
        if (answers is null)
        {
            return string.Empty;
        }

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
