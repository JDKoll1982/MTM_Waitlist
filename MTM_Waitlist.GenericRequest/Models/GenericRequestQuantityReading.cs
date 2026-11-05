using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>Which of the two successful answers a quantity reading carries.</summary>
public enum GenericRequestQuantityKind
{
    /// <summary>The owning system holds a figure, which may be zero.</summary>
    OnHand,
}

/// <summary>
/// The answer to "how much is on hand at this sequence".
/// </summary>
/// <remarks>
/// Three answers have to stay distinguishable: a figure (including zero), not inventoried, and could not be
/// read. Two of them are successes and one is a failure, so a presentation alone cannot express them; the kind
/// beside it can. Zero is a fact and never stands for "not inventoried", and a failed read never stands for
/// either (FR-013, FR-014, FR-017).
/// </remarks>
public sealed record GenericRequestQuantityReading
{
    private GenericRequestQuantityReading(
        GenericRequestQuantityKind kind,
        decimal? figure,
        SequenceSource? answeredBy,
        GenericRequestPartStage stage,
        GenericRequestPresentation presentation,
        DateTimeOffset attemptedAtUtc)
    {
        Kind = kind;
        Figure = figure;
        AnsweredBy = answeredBy;
        Stage = stage;
        Presentation = presentation;
        AttemptedAtUtc = attemptedAtUtc;
    }

    /// <summary>Which successful answer this is. It says nothing when the read failed.</summary>
    public GenericRequestQuantityKind Kind { get; }

    /// <summary>Units on hand. Null when the part is not inventoried or the read failed.</summary>
    public decimal? Figure { get; }

    /// <summary>The system that supplied the figure. Null when the part is not inventoried or the read failed.</summary>
    public SequenceSource? AnsweredBy { get; }

    /// <summary>
    /// The stage the sequence's operation state gave at the moment of this read, which is what the review shows and
    /// what the request records (FR-009, FR-016).
    /// </summary>
    public GenericRequestPartStage Stage { get; }

    /// <summary>Which presentation the quantity region draws.</summary>
    public GenericRequestPresentation Presentation { get; }

    /// <summary>When the read was attempted, in UTC.</summary>
    public DateTimeOffset AttemptedAtUtc { get; }

    /// <summary>Whether the read produced an answer at all.</summary>
    public bool IsAnswered => Presentation == GenericRequestPresentation.Populated;

    /// <summary>The owning system holds a figure, which may be zero.</summary>
    public static GenericRequestQuantityReading OnHand(
        decimal figure,
        SequenceSource answeredBy,
        GenericRequestPartStage stage,
        DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestQuantityKind.OnHand, figure, answeredBy, stage, GenericRequestPresentation.Populated, attemptedAtUtc);

    /// <summary>The owning system could not be reached, or the answer could not be established.</summary>
    public static GenericRequestQuantityReading Unavailable(DateTimeOffset attemptedAtUtc) =>
        new(
            GenericRequestQuantityKind.OnHand,
            null,
            null,
            GenericRequestPartStage.Unknown,
            GenericRequestPresentation.Unavailable,
            attemptedAtUtc);

    /// <summary>The figure as a person reads it, or empty when there is none.</summary>
    public string FigureDisplay => Figure is { } figure
        ? figure.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)
        : string.Empty;

    /// <summary>The answering system's name, or empty when nothing answered.</summary>
    public string AnsweredByDisplay => AnsweredBy is { } source ? SequenceSourceNames.Of(source) : string.Empty;
}
