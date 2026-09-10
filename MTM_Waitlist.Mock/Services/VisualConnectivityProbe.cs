using Microsoft.Data.SqlClient;
using MTM_Waitlist.Mock.Contracts;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Probes Infor Visual by opening a connection and immediately closing it.
/// </summary>
/// <remarks>
/// The probe issues no statement text at all, so it is cheap, cannot be affected by a bad read script,
/// and keeps the SP-first rule intact with no inline-SQL audit exemption.
/// </remarks>
public sealed class VisualConnectivityProbe : IVisualConnectivityProbe
{
    private readonly IVisualConnectionStringProvider _connectionStringProvider;

    /// <summary>Creates the probe.</summary>
    /// <param name="connectionStringProvider">Supplies the Infor Visual connection string.</param>
    public VisualConnectivityProbe(IVisualConnectionStringProvider connectionStringProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionStringProvider);
        _connectionStringProvider = connectionStringProvider;
    }

    /// <inheritdoc />
    public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionStringProvider.Resolve();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Any failure to open the connection means the source is not reachable right now.
            return false;
        }
    }
}
