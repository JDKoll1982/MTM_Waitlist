using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// What one read produced: the presentation to draw, the rows when there are any, and when the read was made.
/// </summary>
/// <remarks>
/// One value rather than three booleans, so an empty read and a failed read can never both be true and the retry
/// control always follows the unavailable state.
/// </remarks>
public abstract class GenericRequestStepResult
{
    protected GenericRequestStepResult(GenericRequestPresentation presentation, DateTimeOffset attemptedAtUtc)
    {
        Presentation = presentation;
        AttemptedAtUtc = attemptedAtUtc;
    }

    /// <summary>Which of the three presentations this result asks for.</summary>
    public GenericRequestPresentation Presentation { get; }

    /// <summary>When the read was made, in UTC.</summary>
    public DateTimeOffset AttemptedAtUtc { get; }

    /// <summary>How many rows this result carries. Zero unless the presentation is populated.</summary>
    public abstract int Count { get; }
}

/// <summary>The typed form of <see cref="GenericRequestStepResult"/>.</summary>
/// <typeparam name="T">The row shape the step draws.</typeparam>
public sealed class GenericRequestStepResult<T> : GenericRequestStepResult
{
    private GenericRequestStepResult(
        GenericRequestPresentation presentation,
        IReadOnlyList<T> items,
        DateTimeOffset attemptedAtUtc)
        : base(presentation, attemptedAtUtc)
    {
        Items = items;
    }

    /// <summary>The rows to draw. Empty unless the presentation is populated.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <inheritdoc />
    public override int Count => Items.Count;

    /// <summary>The read succeeded and found rows.</summary>
    public static GenericRequestStepResult<T> Populated(IReadOnlyList<T> items, DateTimeOffset attemptedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException("A populated result carries at least one row; an empty read is Empty.", nameof(items));
        }

        return new GenericRequestStepResult<T>(GenericRequestPresentation.Populated, items, attemptedAtUtc);
    }

    /// <summary>The read succeeded and found nothing. There is nothing to retry.</summary>
    public static GenericRequestStepResult<T> Empty(DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestPresentation.Empty, Array.Empty<T>(), attemptedAtUtc);

    /// <summary>The read failed. The caller shows the attempt time and a retry control.</summary>
    public static GenericRequestStepResult<T> Unavailable(DateTimeOffset attemptedAtUtc) =>
        new(GenericRequestPresentation.Unavailable, Array.Empty<T>(), attemptedAtUtc);
}
