using System.Collections.Concurrent;
using System.IO;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Whether a picture share is answering, remembered for a short window so a screen that draws many pictures does
/// not pay the same failed lookup once per picture.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this exists for.</b> A picture is looked for on the share whenever this computer holds no copy
/// of it, and that look is a file probe. A share that is not answering does not fail quickly: the name has to be
/// resolved and the attempt has to time out, which measured <b>hundreds of milliseconds to a few seconds</b> on a
/// workstation off the plant network (2026-10-11, feature <c>012-waitlist-card-layout</c>). A card list asks
/// about every part it draws, once per file extension, and the list is rebuilt whenever a request's status
/// changes — so one silent share became a stall of several seconds on every action, and the stall came back
/// whenever the operating system's own cache of the failure lapsed. That is why the report was "it hangs
/// sometimes".
/// </para>
/// <para>
/// <b>Only silence is worth remembering, and only briefly.</b> The verdict is per share, and it is asked before
/// a file probe rather than instead of one: while the share answers, nothing here changes what is probed or how
/// often, so a picture that appears on the share still appears at once. While it does not answer, the probe is
/// skipped and the caller is given the answer the probe would have produced anyway — nothing on the share — so
/// the only difference is the seconds that are no longer spent. The window is short so that a workstation which
/// rejoins the plant network picks the share up again by itself.
/// </para>
/// <para>
/// <b>This is the same shape as <c>MySqlHostFallback</c></b>, which answers the same question for the database
/// hosts: probe once, remember the answer for a window, and re-probe when the window lapses rather than on every
/// single operation.
/// </para>
/// </remarks>
public static class PictureShareReachability
{
    /// <summary>
    /// How long a verdict stands before the share is probed again. Long enough to collapse the probes of one
    /// rebuild and of the actions that follow it, short enough that a share which comes back is noticed without
    /// anybody restarting the application.
    /// </summary>
    private static readonly TimeSpan s_verdictLifetime = TimeSpan.FromSeconds(30);

    private static readonly ConcurrentDictionary<string, (bool Reachable, DateTimeOffset ProbedUtc)> s_verdicts =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the share a path belongs to is answering, probing it at most once per
    /// <see cref="VerdictLifetime"/>.
    /// </summary>
    /// <param name="shareRoot">The share root, from <see cref="ShareRootOf"/>. Blank means nothing to judge.</param>
    /// <returns>
    /// <see langword="true" /> when the share answered, and also when there is no share to judge — a local drive,
    /// or a path this cannot read a root from. Only a share that was probed and did not answer is false.
    /// </returns>
    public static bool IsReachable(string? shareRoot) =>
        IsReachable(shareRoot, Directory.Exists, DateTimeOffset.UtcNow);

    /// <summary>
    /// The same question, with the probe and the clock supplied, so the cache's own rule can be exercised without
    /// a share and without waiting.
    /// </summary>
    /// <param name="shareRoot">The share root to judge.</param>
    /// <param name="probeRoot">Reports whether the share root answers.</param>
    /// <param name="now">The moment the question is being asked.</param>
    /// <returns>The remembered verdict, or the freshly probed one.</returns>
    internal static bool IsReachable(string? shareRoot, Func<string, bool> probeRoot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(probeRoot);

        if (string.IsNullOrWhiteSpace(shareRoot))
        {
            return true;
        }

        var key = shareRoot.Trim();

        if (s_verdicts.TryGetValue(key, out var verdict) && ShouldProbe(verdict.ProbedUtc, now) is false)
        {
            return verdict.Reachable;
        }

        var reachable = probeRoot(key);
        s_verdicts[key] = (reachable, now);

        return reachable;
    }

    /// <summary>
    /// Whether the share has to be probed again: when it has not been asked about yet, or when the verdict it
    /// gave has lapsed. Pure, so the window's own behaviour is checkable at its two boundaries.
    /// </summary>
    /// <param name="probedUtc">When the share was last probed, or null when it never was.</param>
    /// <param name="now">The moment the question is being asked.</param>
    internal static bool ShouldProbe(DateTimeOffset? probedUtc, DateTimeOffset now) =>
        probedUtc is null || now - probedUtc.Value >= s_verdictLifetime;

    /// <summary>How long a verdict stands. Exposed so a test states the window rather than guessing it.</summary>
    internal static TimeSpan VerdictLifetime => s_verdictLifetime;

    /// <summary>
    /// The share a path is read from: the server and the share name for a network path, the drive for a local
    /// one, and nothing for a path this cannot root.
    /// </summary>
    /// <param name="path">The picture path about to be probed.</param>
    /// <returns>The share root, or <see langword="null" /> when there is nothing worth asking about.</returns>
    /// <remarks>
    /// The verdict is taken on the share rather than on the file, because it is the share that is silent: every
    /// file on an unreachable share fails the same way, so one answer stands for all of them. A local path is
    /// answered by its drive, which is always there, so nothing local is ever skipped.
    /// </remarks>
    public static string? ShareRootOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim();

        // A stored path may use either separator, and a share spelled with forward slashes names the same share.
        // Unifying them first is what stops one share being judged twice, and judged differently.
        var normalized = trimmed.Replace('/', '\\');

        if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var segments = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            // A bare "\\server" names no share, so there is nothing a file probe could have been aimed at.
            return segments.Length < 2 ? null : $@"\\{segments[0]}\{segments[1]}";
        }

        // A path that is not rooted has no share to ask about: the application directory is where it would be
        // read from, and that is this machine.
        if (Path.IsPathRooted(normalized) is false)
        {
            return null;
        }

        var root = Path.GetPathRoot(normalized);

        return string.IsNullOrWhiteSpace(root) ? null : root;
    }

    /// <summary>
    /// Clears the remembered verdicts, so a test starts from a known state and the next question is probed.
    /// </summary>
    internal static void Forget() => s_verdicts.Clear();
}
