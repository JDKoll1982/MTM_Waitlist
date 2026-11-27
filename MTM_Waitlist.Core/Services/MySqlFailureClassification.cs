using MySqlConnector;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Tells a failure that a retry can fix from one it cannot, so the seam spends its retry budget only where the
/// budget can change the outcome.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this exists for.</b> A read against a store that does not hold the procedure it names failed
/// three times, with about three seconds of delay between the attempts, and was then reported to the screen as
/// "the store did not answer". Neither statement was true: the store answered — at once, three times — saying the
/// same thing each time, that the procedure is not there. Measured on 2026-11-27 from a captured application log:
/// 132 first-chance exception lines for one missing procedure, and a page change that stalled for the whole retry
/// schedule.
/// </para>
/// <para>
/// <b>Only permanent answers are short-circuited.</b> Everything else — a refused connection, a timeout, a server
/// that is starting, a deadlock that a retry may genuinely clear — keeps the bounded retry the availability
/// contract describes. The seam still returns its neutral result rather than throwing, because the contract a
/// screen relies on is unchanged; what changes is that it returns after one honest attempt and says what is
/// actually wrong, so the person is pointed at a deployment rather than at a network.
/// </para>
/// </remarks>
internal static class MySqlFailureClassification
{
    /// <summary>
    /// Why this failure cannot be fixed by trying again, or <see langword="null" /> when a retry might help.
    /// </summary>
    /// <param name="exception">The failure the seam caught.</param>
    /// <returns>A short phrase naming the permanent cause, or <see langword="null" />.</returns>
    public static string? PermanentReason(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: > 0 } mySql && ReasonFor(mySql.Number) is { } reason)
            {
                return reason;
            }
        }

        return null;
    }

    /// <summary>
    /// The permanent causes, by the server's own error number.
    /// </summary>
    /// <param name="number">A MySQL error number.</param>
    /// <returns>A short phrase naming the cause, or <see langword="null" /> when the number is not permanent.</returns>
    /// <remarks>
    /// These are the numbers a retry cannot change: the object or database is absent, or this login may not use
    /// it. A connection failure is deliberately absent from the list — that is the case the retry exists for.
    /// </remarks>
    public static string? ReasonFor(int number) => number switch
    {
        1044 => "this login may not use that database",
        1045 => "the server refused this login",
        1049 => "that database does not exist on the server",
        1054 => "the statement names a column that does not exist",
        1064 => "the statement is not valid for this server",
        1142 => "this login may not run that object",
        1143 => "this login may not read that column",
        1146 => "the table does not exist on the server",
        1305 => "the procedure does not exist on the server",
        _ => null,
    };
}
