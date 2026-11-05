namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The dunnage answer a generic request records on itself, and reads back from its own stored answers.
/// </summary>
/// <remarks>
/// <para>
/// A generic request has no job, so the request itself has to carry the dunnage part the requester chose, its type,
/// its picture and whether it was still in the catalogue when the request was raised. These values ride on the
/// request's existing <c>answers_json</c> column through the same insert and list procedures every request already
/// uses, so nothing in the schema changes.
/// </para>
/// <para>
/// It mirrors <see cref="GenericRequestPartAnswer"/> in the same folder: const keys, a case-insensitive dictionary,
/// and a read that answers nothing rather than throwing when a request states no dunnage.
/// </para>
/// </remarks>
public sealed record GenericRequestDunnageAnswer
{
    /// <summary>The chosen part's part number.</summary>
    public const string PartKey = "dunnage.part";

    /// <summary>The chosen part's dunnage type name.</summary>
    public const string TypeKey = "dunnage.type";

    /// <summary>The picture reference recorded for the part, or blank when it has none.</summary>
    public const string PictureKey = "dunnage.picture";

    /// <summary>Whether the part was still in the catalogue when the request was raised.</summary>
    public const string OfferedKey = "dunnage.offered";

    public string PartNumber { get; init; } = string.Empty;

    public string TypeName { get; init; } = string.Empty;

    /// <summary>The part's picture as an absolute path, or blank when the part has none.</summary>
    public string PicturePath { get; init; } = string.Empty;

    /// <summary>
    /// Whether the part was still offered at raise time. False is the fact that a chosen part has left the
    /// catalogue; the request keeps the part and says so instead of dropping it.
    /// </summary>
    public bool Offered { get; init; } = true;

    /// <summary>The answers a request records for this dunnage answer, ready for <c>answers_json</c>.</summary>
    public Dictionary<string, string> ToAnswers() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [PartKey] = PartNumber,
            [TypeKey] = TypeName,
            [PictureKey] = PicturePath,
            [OfferedKey] = Offered ? "true" : "false",
        };

    /// <summary>
    /// Read the dunnage answer back from a request's stored answers.
    /// </summary>
    /// <param name="answers">The request's stored answers, or <c>null</c>.</param>
    /// <returns>The answer, or <c>null</c> when the request states no dunnage or the record cannot be read.</returns>
    /// <remarks>
    /// A missing dictionary, a missing or blank part number, or an offered value that is not a boolean all answer
    /// nothing. The card and the detail then draw no dunnage claim rather than a wrong one, and no caller has to
    /// guard against an exception coming out of a stored row.
    /// </remarks>
    public static GenericRequestDunnageAnswer? TryRead(IReadOnlyDictionary<string, string>? answers)
    {
        if (answers is null)
        {
            return null;
        }

        var partNumber = Read(answers, PartKey).Trim();
        if (partNumber.Length == 0)
        {
            return null;
        }

        var offeredText = Read(answers, OfferedKey).Trim();
        var offered = true;
        if (offeredText.Length > 0 && !bool.TryParse(offeredText, out offered))
        {
            return null;
        }

        return new GenericRequestDunnageAnswer
        {
            PartNumber = partNumber,
            TypeName = Read(answers, TypeKey).Trim(),
            PicturePath = Read(answers, PictureKey).Trim(),
            Offered = offered,
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
