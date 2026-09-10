using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Runs an Infor Visual read script and classifies the outcome.
/// </summary>
/// <remarks>
/// Owned by <c>MTM_Waitlist.Mock</c> so every read shape has exactly one live-read implementation
/// (replacing the retired Module_Setup and Module_Core copies).
/// </remarks>
public interface IVisualQueryExecutor
{
    /// <summary>
    /// Executes the script at <paramref name="sourceScriptRelativePath"/> against Infor Visual.
    /// </summary>
    /// <param name="sourceScriptRelativePath">
    /// Repository-relative script path, for example
    /// <c>Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql</c>.
    /// </param>
    /// <param name="parameters">Script parameters, with or without the leading <c>@</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="VisualQueryStatus.Ok"/> for a completed read (even with zero rows),
    /// <see cref="VisualQueryStatus.Unreachable"/> when the source could not be reached, or
    /// <see cref="VisualQueryStatus.Failed"/> for any other fault.
    /// </returns>
    Task<VisualQueryOutcome> ExecuteAsync(
        string sourceScriptRelativePath,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
