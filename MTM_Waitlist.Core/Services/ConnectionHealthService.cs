using Microsoft.Data.SqlClient;
using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IConnectionHealthService"/>
/// <remarks>
/// Probes each external source with a short-open connection test. Results are cached as the
/// "last known" snapshot. Debouncing/flapping policy is handled by the caller (mock-routing layer),
/// not here.
/// </remarks>
public sealed class ConnectionHealthService : IConnectionHealthService
{
    private const int DefaultTimeoutSeconds = 5;
    private const int ReceivingTimeoutSeconds = 5;
    private const int InforTimeoutSeconds = 5;

    private readonly IExternalConnectionInfoProvider _connectionInfoProvider;
    private readonly Dictionary<ConnectionSource, ConnectionHealthState> _lastKnown =
        new();
    private readonly object _lock = new();

    public ConnectionHealthService(IExternalConnectionInfoProvider connectionInfoProvider)
    {
        _connectionInfoProvider = connectionInfoProvider;
    }

    public async Task<ConnectionHealthState> CheckAsync(ConnectionSource source, CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionInfoProvider.ResolveConnectionString(source);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var notConfigured = new ConnectionHealthState
            {
                Source = source,
                Status = ConnectionHealthStatus.Unreachable,
                CheckedUtc = DateTimeOffset.UtcNow,
                Reason = "Not configured.",
            };
            SetLastKnown(notConfigured);
            return notConfigured;
        }

        var status = ConnectionHealthStatus.Unreachable;
        var reason = string.Empty;
        try
        {
            var reachable = _connectionInfoProvider.IsSqlServer(source)
                ? await ProbeSqlServerAsync(connectionString, cancellationToken).ConfigureAwait(false)
                : await ProbeMySqlAsync(connectionString, cancellationToken).ConfigureAwait(false);

            status = reachable ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Info("ConnectionHealth", $"Health probe failed for '{source}'. {ex.GetType().Name}: {ex.Message}");
            reason = ClassifyException(ex);
        }

        var state = new ConnectionHealthState
        {
            Source = source,
            Status = status,
            CheckedUtc = DateTimeOffset.UtcNow,
            Reason = reason,
        };
        SetLastKnown(state);
        StartupDebugLog.Info("ConnectionHealth", $"Health check for '{source}' = {state.Status}.");
        return state;
    }

    public async Task<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var infor = await CheckAsync(ConnectionSource.InforVisual, cancellationToken).ConfigureAwait(false);
        var receiving = await CheckAsync(ConnectionSource.Receiving, cancellationToken).ConfigureAwait(false);
        return new Dictionary<ConnectionSource, ConnectionHealthState>
        {
            [ConnectionSource.InforVisual] = infor,
            [ConnectionSource.Receiving] = receiving,
        };
    }

    public ConnectionHealthState GetLastKnown(ConnectionSource source)
    {
        lock (_lock)
        {
            return _lastKnown.TryGetValue(source, out var state)
                ? state
                : new ConnectionHealthState { Source = source, Status = ConnectionHealthStatus.Unknown };
        }
    }

    private void SetLastKnown(ConnectionHealthState state)
    {
        lock (_lock)
        {
            _lastKnown[state.Source] = state;
        }
    }

    private static async Task<bool> ProbeMySqlAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString) { ConnectionTimeout = (uint)ReceivingTimeoutSeconds };
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection.State == System.Data.ConnectionState.Open;
    }

    private static async Task<bool> ProbeSqlServerAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = InforTimeoutSeconds,
            TrustServerCertificate = true,
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection.State == System.Data.ConnectionState.Open;
    }

    private static string ClassifyException(Exception ex) => ex switch
    {
        SqlException { Number: 18456 } => "Bad login for the Infor Visual database.",
        SqlException => "Infor Visual server unreachable.",
        MySqlException { Number: 1045 } => "Bad login for the receiving database.",
        MySqlException { Number: 1042 } or MySqlException { Number: 1044 } => "Receiving server unreachable.",
        MySqlException => "Receiving database unreachable.",
        _ => string.Empty,
    };
}
