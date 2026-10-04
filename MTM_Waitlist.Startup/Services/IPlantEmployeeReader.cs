namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// One person as the plant system knows them, by employee number
/// (`contracts/employee-read-shape-contract.md`; FR-004, FR-007, FR-013).
/// </summary>
/// <param name="EmployeeNumber">The number the lookup was made by.</param>
/// <param name="FirstName">The first name the plant system answered.</param>
/// <param name="LastName">The last name the plant system answered.</param>
public sealed record PlantEmployee(string EmployeeNumber, string FirstName, string LastName);

/// <summary>
/// What a plant-system lookup answered: the person, or a plain statement that the number is not held, or a plain
/// statement that the plant system could not be read at all (FR-013).
/// </summary>
/// <remarks>
/// <b>"Not held" and "could not be asked" are different answers.</b> FR-013 requires set-up to say it cannot be
/// completed right now when the plant system is unreachable and the mirror has never held the employee, which is
/// not the same as the plant system answering that it does not employ that number. Collapsing the two would make
/// the application state a refusal the person did not earn.
/// </remarks>
/// <param name="Employee">The person, or <c>null</c> when the number was not held.</param>
/// <param name="WasReadable">
/// Whether the plant system (or its mirror) could be read at all. <c>false</c> means the answer is unknown and
/// set-up must say it cannot be completed right now.
/// </param>
public sealed record PlantEmployeeLookup(PlantEmployee? Employee, bool WasReadable)
{
    /// <summary>The plant system (or its mirror) answered that it does not hold the number.</summary>
    public static PlantEmployeeLookup NotHeld { get; } = new(null, true);

    /// <summary>The plant system could not be read and the mirror has never held the number.</summary>
    public static PlantEmployeeLookup Unreadable { get; } = new(null, false);

    /// <summary>The plant system holds the person.</summary>
    /// <param name="employee">The person.</param>
    public static PlantEmployeeLookup Found(PlantEmployee employee) => new(employee, true);
}

/// <summary>
/// Reads one person from the plant system by employee number, through the sixth read shape
/// (`contracts/employee-read-shape-contract.md`; FR-004, FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>The read is the cached fallback, transparently.</b> The implementation is the sixth shape's
/// <c>IVisualReadFallback</c>, so a live read is attempted and the mirror answers only when the plant system is
/// unreachable — the caller cannot tell which answered, which is what FR-032 asks for. The abstraction exists
/// here because the startup module must not reference the mock library; the composition root bridges the two.
/// </para>
/// <para>
/// <b>The read never throws for an unreachable plant system.</b> It answers
/// <see cref="PlantEmployeeLookup.Unreadable"/>, so the caller reports "cannot be completed right now" rather
/// than a crash (FR-013).
/// </para>
/// </remarks>
public interface IPlantEmployeeReader
{
    /// <summary>
    /// Reads the person the plant system holds for <paramref name="employeeNumber"/>.
    /// </summary>
    /// <param name="employeeNumber">The four digits the person typed.</param>
    /// <param name="cancellationToken">Cancels the read when the launch's stated maximum passes.</param>
    Task<PlantEmployeeLookup> FindByNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
}
