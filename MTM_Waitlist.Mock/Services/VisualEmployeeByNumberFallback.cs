using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>Shape 6, <c>employee_by_number</c>: one employee's name, resolved from their number.</summary>
public sealed class VisualEmployeeByNumberFallback
    : VisualReadFallback<VisualEmployeeByNumberRequest, VisualEmployeeByNumberRow>
{
    private static readonly VisualReadShape s_shape = ResolveShape("employee_by_number");

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">Reads the <c>mtm_mock</c> mirror.</param>
    /// <param name="reachability">Supplies the settled reachability verdict the shared read algorithm honours.</param>
    public VisualEmployeeByNumberFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer = null,
        IVisualReachabilityDetector? reachability = null)
        : base(executor, mySqlHelperServer, reachability)
    {
    }

    /// <inheritdoc />
    protected override VisualReadShape Shape => s_shape;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildLiveParameters(VisualEmployeeByNumberRequest request) =>
        new Dictionary<string, object?>
        {
            ["EmployeeNumber"] = request.EmployeeNumber,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> BuildCacheParameters(VisualEmployeeByNumberRequest request) =>
        new Dictionary<string, object?>
        {
            ["p_employee_number"] = request.EmployeeNumber,
        };

    /// <inheritdoc />
    protected override VisualEmployeeByNumberRow MapRow(IReadOnlyDictionary<string, object?> row) => new()
    {
        EmployeeNumber = VisualRowReader.GetString(row, "EmployeeNumber"),
        FirstName = VisualRowReader.GetString(row, "FirstName"),
        LastName = VisualRowReader.GetString(row, "LastName"),
    };
}
