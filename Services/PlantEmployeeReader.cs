using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Services;

/// <summary>
/// The composition root's bridge between the startup module's plant-system read and the sixth read shape's
/// cached fallback (`contracts/employee-read-shape-contract.md`; FR-004, FR-013, FR-032).
/// </summary>
/// <remarks>
/// <para>
/// <b>The bridge exists because the startup module must not reference the mock library.</b> The startup module
/// declares the abstraction it needs (<see cref="IPlantEmployeeReader"/>) and this class, which lives where both
/// libraries are visible, implements it over the shape's own <c>IVisualReadFallback</c>. No read logic is
/// duplicated: the live attempt, the mirror, the settled verdict and the identical row shape are all the
/// fallback's, exactly as they are for the other five shapes.
/// </para>
/// <para>
/// <b>"Not held" and "could not be asked" are kept apart.</b> A live read that answers nothing is the plant
/// system saying it does not hold the number; a mirrored read that answers nothing means the plant system could
/// not be reached and the mirror has never held it, which set-up reports as "cannot be completed right now"
/// rather than as a refusal (FR-013).
/// </para>
/// </remarks>
internal sealed class PlantEmployeeReader : IPlantEmployeeReader
{
    private readonly IVisualReadFallback<VisualEmployeeByNumberRequest, VisualEmployeeByNumberRow> _fallback;

    /// <summary>Creates the bridge over the sixth shape's read fallback.</summary>
    /// <param name="fallback">The shape's own live-then-cached read.</param>
    public PlantEmployeeReader(
        IVisualReadFallback<VisualEmployeeByNumberRequest, VisualEmployeeByNumberRow> fallback)
        => _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));

    /// <inheritdoc />
    public async Task<PlantEmployeeLookup> FindByNumberAsync(
        string employeeNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = employeeNumber?.Trim() ?? string.Empty;

        if (normalized.Length == 0)
        {
            // Nothing was asked for, so the plant system is not asked either.
            return PlantEmployeeLookup.NotHeld;
        }

        try
        {
            var result = await _fallback
                .ReadWithProvenanceAsync(new VisualEmployeeByNumberRequest(normalized), cancellationToken)
                .ConfigureAwait(false);

            var row = result.Value.FirstOrDefault();

            if (row is null)
            {
                // The mirror answering nothing is the plant system being unreachable with nothing to fall back
                // on; a live read answering nothing is a real "we do not employ that number".
                return result.Source == VisualReadSource.ServedFromCache
                    ? PlantEmployeeLookup.Unreadable
                    : PlantEmployeeLookup.NotHeld;
            }

            var agreed = EmployeeByNumberRow.From(row);

            return PlantEmployeeLookup.Found(new PlantEmployee(
                string.IsNullOrWhiteSpace(agreed.EmployeeNumber) ? normalized : agreed.EmployeeNumber,
                agreed.FirstName,
                agreed.LastName));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not an unreadable plant system.
            throw;
        }
        catch (Exception)
        {
            // A read that could not be made has confirmed nothing, and the caller says so in plain words rather
            // than showing the provider's message to the person (FR-013).
            return PlantEmployeeLookup.Unreadable;
        }
    }
}
