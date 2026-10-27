namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The one spelling of "every building", offered as the last choice wherever a building is chosen.
/// </summary>
/// <remarks>
/// <para>
/// Two screens filter on a building and a third files a request under one, so the value that means "no building
/// filter" is spelled once here rather than repeated as a literal at each of them.
/// </para>
/// <para>
/// It is deliberately not a building the catalog holds: nothing writes it to a work centre or to a computer, and
/// a request is always filed under the building of the work centre it names
/// (<see cref="AsFilter"/> is what turns the choice back into "no filter").
/// </para>
/// </remarks>
public static class BuildingScope
{
    /// <summary>The choice that means every building, shown as the last entry in a building chooser.</summary>
    public const string AllBuildings = "All Buildings";

    /// <summary>Whether the given choice is <see cref="AllBuildings"/>.</summary>
    public static bool IsAllBuildings(string? building) =>
        string.Equals(building?.Trim(), AllBuildings, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The value a building filter should be given: the building itself, or <c>null</c> when every building is
    /// wanted. A blank choice is treated as every building, which is what the filter reads a null as.
    /// </summary>
    public static string? AsFilter(string? building) =>
        string.IsNullOrWhiteSpace(building) || IsAllBuildings(building) ? null : building.Trim();
}
