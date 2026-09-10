namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// How an attempted Infor Visual read ended.
/// </summary>
/// <remarks>
/// The distinction is load-bearing: only <see cref="Unreachable"/> may fall back to the mirror.
/// <see cref="Ok"/> with zero rows is a real answer, and <see cref="Failed"/> must surface.
/// </remarks>
public enum VisualQueryStatus
{
    /// <summary>The query ran and produced a result, which may legitimately be empty.</summary>
    Ok,

    /// <summary>The source could not be reached (connect, timeout, or login failure).</summary>
    Unreachable,

    /// <summary>The source was reached but the read failed, or the read could not be set up.</summary>
    Failed
}
