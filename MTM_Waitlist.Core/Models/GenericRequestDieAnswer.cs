namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The die a generic requester chose, and reads back from the request's own stored answers.
/// </summary>
/// <remarks>
/// <para>
/// A generic work center has no job to read a die from, so the requester searches Infor Visual and the request
/// carries what they chose. The value rides on the request's existing <c>answers_json</c> column through the same
/// insert and list procedures every request already uses, so nothing in the schema changes (FR-011).
/// </para>
/// <para>
/// The location is kept beside the number because the board draws a card from a request and no longer has a job to
/// look the die up in: without the stored location a generic die card could name the die but never where it lives
/// (FR-012). A die Visual records no location for writes no location key rather than an empty one.
/// </para>
/// <para>
/// It mirrors <see cref="GenericRequestScrapAnswer"/> in the same folder: const keys, a case-insensitive dictionary,
/// and a read that answers nothing rather than throwing when a request states no die.
/// </para>
/// </remarks>
public sealed record GenericRequestDieAnswer
{
    /// <summary>The die's Infor Visual number.</summary>
    public const string DieNumberKey = "die.number";

    /// <summary>Where the die lives, as the search resolved it.</summary>
    public const string DieLocationKey = "die.location";

    /// <summary>The chosen die's number, or blank when the request states no die.</summary>
    public string DieNumber { get; init; } = string.Empty;

    /// <summary>The chosen die's location, or blank when Visual records none.</summary>
    public string DieLocation { get; init; } = string.Empty;

    /// <summary>Whether the die's location is known, which is what the card draws on.</summary>
    public bool HasLocation => !string.IsNullOrWhiteSpace(DieLocation);

    /// <summary>
    /// The answers a request records for this die answer, ready for <c>answers_json</c>.
    /// </summary>
    /// <remarks>
    /// A blank number writes no key at all, so a request that never reached the step records nothing about a die
    /// rather than an empty answer that reads as a choice. An unknown location writes only the number.
    /// </remarks>
    public Dictionary<string, string> ToAnswers()
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(DieNumber))
        {
            return answers;
        }

        answers[DieNumberKey] = DieNumber.Trim();

        if (HasLocation)
        {
            answers[DieLocationKey] = DieLocation.Trim();
        }

        return answers;
    }

    /// <summary>
    /// Read the die answer back from a request's stored answers.
    /// </summary>
    /// <param name="answers">The request's stored answers, or <c>null</c>.</param>
    /// <returns>The answer, or <c>null</c> when the request states no die.</returns>
    public static GenericRequestDieAnswer? TryRead(IReadOnlyDictionary<string, string>? answers)
    {
        if (answers is null)
        {
            return null;
        }

        var dieNumber = Read(answers, DieNumberKey).Trim();
        if (dieNumber.Length == 0)
        {
            return null;
        }

        return new GenericRequestDieAnswer
        {
            DieNumber = dieNumber,
            DieLocation = Read(answers, DieLocationKey).Trim(),
        };
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
