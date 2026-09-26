using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// The static logging facade the retired static-debug-log call sites were repointed to (contract §5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Unconditional.</b> There is no <c>[Conditional]</c> attribute and no <c>#if DEBUG</c> guard anywhere on
/// this type. The type it replaces was marked <c>[Conditional("DEBUG")]</c>, so a Release build compiled its 482
/// calls — and their arguments — out entirely and a shop-floor machine recorded nothing at all (SC-003, plan D6).
/// A façade that kept the attribute would leave the store empty in exactly the build that matters.
/// </para>
/// <para>
/// <b>Forwarding only.</b> Every member forwards to <see cref="IAppLogSink"/>, which is the
/// shape <c>MTM_Waitlist.Module_Logging.ILogService</c> satisfies. Nothing is composed, serialized or queued
/// here: the seam does all of that, so a call site still supplies only what it can know (FR-021, FR-035).
/// </para>
/// <para>
/// <b>Silent when nothing is listening.</b> The sink is null until <see cref="Configure"/> is called, and a call
/// with no sink returns without doing anything — no throw and no conditional-compilation branch. That is what
/// lets the 1400-odd tests, which run with no store and never configure a sink, keep passing unchanged.
/// </para>
/// <para>
/// <b>Why the façade lives in <c>MTM_Waitlist.Core</c> and in this namespace.</b> The logging module references
/// <c>MTM_Waitlist.Core</c>, so the façade cannot live in the module that the module libraries would have to
/// reference back. Placing it in <c>MTM_Waitlist.Module_Core.Helpers</c> — the namespace the retired type
/// occupied and that every one of the 76 call-site files already imports — is what lets the migration be a pure
/// token replacement with no per-file using edit anywhere.
/// </para>
/// </remarks>
public static class AppLog
{
    /// <summary>The seam being forwarded to, or null when no host has initialised one.</summary>
    private static IAppLogSink? s_sink;

    /// <summary>
    /// Points the façade at the seam, or clears it.
    /// </summary>
    /// <param name="sink">
    /// The logging seam resolved from the host's container, or null to detach it. Accepting null is deliberate: a
    /// test tears its recording sink down by disposing of it this way.
    /// </param>
    public static void Configure(IAppLogSink? sink) => s_sink = sink;

    /// <summary>
    /// Records that something happened as the caller expected.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    public static void Info(string module, string message) => s_sink?.Info(module, message);

    /// <summary>
    /// Records that the operation under way failed, with the fault that failed it.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="exception">The fault, when there is one.</param>
    /// <param name="message">What the entry says.</param>
    /// <remarks>
    /// The argument order is the retired type's, not the seam's, because the migration repoints 129 <c>Error</c>
    /// call sites by replacing the type token alone and the arguments are not touched. The order is corrected in
    /// the one place it can be: here. The <c>Info</c> order is the same in both, so no member needs translating.
    /// </remarks>
    public static void Error(string module, Exception? exception, string message) =>
        s_sink?.Error(module, message, exception);
}
