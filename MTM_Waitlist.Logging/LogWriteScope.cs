namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// Marks the flow that is currently writing an entry to the store, so the recorder cannot record itself
/// (`contracts/logging-contract.md` §1.4; FR-037, SC-016).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is needed at all.</b> The store write goes through the application's data-access seam, and that
/// seam logs what it does. So writing one entry raises a log call, which would queue another entry, which would
/// write and raise another. On 2026-09-27 the first real launch of the rebuilt surface did exactly that: one
/// application wrote 21,663 entries in 55 seconds, every one of them the log writer announcing its own write of
/// <c>sp_ops_startup_logs_insert</c>. The application was so busy recording itself that it never got as far as
/// its first launch step.
/// </para>
/// <para>
/// <b>Why the guard is a flow and not a flag on the writer.</b> A plain lock or boolean would drop legitimate
/// entries raised at the same moment by another part of the application, and it would deadlock if the seam
/// logged synchronously. <see cref="AsyncLocal{T}"/> marks the one execution flow that is inside the write, and
/// nothing else: an entry raised by a launch step while a write is in flight is on a different flow and is kept,
/// which is precisely the entry a support reader wants.
/// </para>
/// <para>
/// <b>The entry is dropped, not deferred.</b> Deferring would re-enter the queue from inside the drain loop and
/// would grow without bound while a store wrote slowly. Dropping is what the contract already says happens to an
/// entry the recorder cannot keep: nothing is written to the machine in its place, and nothing is said about it.
/// </para>
/// </remarks>
internal static class LogWriteScope
{
    private static readonly AsyncLocal<int> s_depth = new();

    /// <summary>Whether the current flow is inside a write to the log store.</summary>
    internal static bool IsWriting => s_depth.Value > 0;

    /// <summary>
    /// Marks the current flow as writing, until the returned scope is disposed.
    /// </summary>
    /// <returns>A scope that clears the mark when it is disposed.</returns>
    internal static IDisposable Enter()
    {
        s_depth.Value++;

        return new Scope();
    }

    /// <summary>Clears one level of the mark.</summary>
    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            s_depth.Value--;
        }
    }
}
