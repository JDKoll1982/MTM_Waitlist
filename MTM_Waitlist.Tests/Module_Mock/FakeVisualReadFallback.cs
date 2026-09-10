using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Test double for a read-shape fallback: returns canned rows with no live source and no cache.
/// </summary>
/// <typeparam name="TRequest">The shape's input.</typeparam>
/// <typeparam name="TRow">The shape's row type.</typeparam>
public sealed class FakeVisualReadFallback<TRequest, TRow> : IVisualReadFallback<TRequest, TRow>
{
    private readonly Func<TRequest, IReadOnlyList<TRow>> _rowsFactory;

    /// <summary>Creates a fallback that derives its rows from the request.</summary>
    public FakeVisualReadFallback(Func<TRequest, IReadOnlyList<TRow>> rowsFactory)
    {
        ArgumentNullException.ThrowIfNull(rowsFactory);
        _rowsFactory = rowsFactory;
    }

    /// <summary>Creates a fallback that always returns the same rows.</summary>
    public FakeVisualReadFallback(IReadOnlyList<TRow> rows)
        : this(_ => rows)
    {
    }

    /// <summary>How many times a read was attempted, so tests can assert short-circuits.</summary>
    public int ReadCount { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<TRow>> ReadAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(_rowsFactory(request));
    }

    /// <inheritdoc />
    public Task<CachedReadResult<IReadOnlyList<TRow>>> ReadWithProvenanceAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(new CachedReadResult<IReadOnlyList<TRow>>
        {
            Value = _rowsFactory(request),
            Source = VisualReadSource.ServedFromLive,
        });
    }
}
