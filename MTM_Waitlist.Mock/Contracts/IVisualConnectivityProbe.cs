namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// A lightweight Infor Visual connectivity check.
/// </summary>
/// <remarks>
/// The probe opens a connection and nothing more: it never executes a read shape and returns no rows,
/// so it stays cheap enough to run on a schedule. It carries no SQL statement text, which keeps the
/// SP-first rule intact with no audit exemption.
/// </remarks>
public interface IVisualConnectivityProbe
{
    /// <summary>Returns <see langword="true"/> when Infor Visual accepted a connection.</summary>
    Task<bool> ProbeAsync(CancellationToken cancellationToken = default);
}
