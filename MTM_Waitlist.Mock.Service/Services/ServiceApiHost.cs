using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Api;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Hosts the service's network API in-process (FR-011, FR-012).
/// </summary>
/// <remarks>
/// <para>
/// <b>Restore is independent of this host.</b> The host never resolves <see cref="RestoreService"/> and
/// no route reaches it, so the API can run on a machine where restore is not registered, and a valid
/// token still cannot restore a store (FR-010/FR-023).
/// </para>
/// <para>
/// The listener is bound to the configured address/port. A bind failure is reported to the caller instead
/// of being swallowed: an operator who moved the port must find out, but a busy port must not stop the
/// refresh and backup engines from running.
/// </para>
/// <para>
/// Startup is deliberately non-fatal for the rest of the app: the tray, the refresh loop, and the backup
/// schedule keep working when the API cannot listen.
/// </para>
/// </remarks>
public sealed class ServiceApiHost : IAsyncDisposable
{
    private readonly ServiceApiOperations _operations;
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly ILogger<ServiceApiHost> _logger;
    private readonly TimeProvider _timeProvider;

    private WebApplication? _application;

    /// <summary>Creates the host.</summary>
    /// <param name="operations">The operations facade the routes delegate to.</param>
    /// <param name="configurationStore">
    /// Supplies the binding and the credential. The store is registered in the API's own container so the
    /// authentication handler resolves it from the same instance the service uses.
    /// </param>
    /// <param name="logger">Logger.</param>
    /// <param name="timeProvider">Time source.</param>
    public ServiceApiHost(
        ServiceApiOperations operations,
        ServiceConfigurationStore configurationStore,
        ILogger<ServiceApiHost> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(configurationStore);
        ArgumentNullException.ThrowIfNull(logger);

        _operations = operations;
        _configurationStore = configurationStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Whether the API is currently listening.</summary>
    public bool IsRunning => _application is not null;

    /// <summary>The address the API is (or would be) bound to, for reporting.</summary>
    public string BindEndpoint =>
        $"{_configurationStore.Current.Api.BindAddress}:{_configurationStore.Current.Api.Port}";

    /// <summary>
    /// Starts the API listener.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the listener started; <see langword="false"/> when it could not.</returns>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null)
        {
            return true;
        }

        if (!_configurationStore.HasCredential)
        {
            // Refuse to open a listener that no client could ever authenticate against.
            _logger.LogError("The service API was not started because no shared credential has been generated yet.");
            return false;
        }

        var settings = _configurationStore.Current.Api;

        if (!IPAddress.TryParse(settings.BindAddress, out var address))
        {
            _logger.LogError(
                "The service API was not started because the bind address '{BindAddress}' is not a valid IP address.",
                settings.BindAddress);
            return false;
        }

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new ForwardingLoggerProvider(_logger));

        builder.WebHost.ConfigureKestrel(options => options.Listen(address, settings.Port));

        builder.Services
            .AddAuthentication(SharedTokenAuthenticationHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SharedTokenAuthenticationHandler>(
                SharedTokenAuthenticationHandler.SchemeName,
                _ => { });

        builder.Services.AddAuthorization(options =>
        {
            // Fallback policy: any route that forgets an explicit requirement is still gated (SC-010).
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    SharedTokenAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddSingleton(_operations);
        builder.Services.AddSingleton(_configurationStore);

        var application = builder.Build();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapServiceApi();

        try
        {
            await application.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "The service API could not bind to {BindEndpoint}; the refresh and backup engines are unaffected.",
                BindEndpoint);

            await application.DisposeAsync().ConfigureAwait(false);
            return false;
        }

        _application = application;
        _logger.LogInformation("Service API listening on {BindEndpoint}.", BindEndpoint);
        return true;
    }

    /// <summary>Stops the listener, if one is running.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var application = _application;
        if (application is null)
        {
            return;
        }

        _application = null;

        try
        {
            await application.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "The service API did not stop cleanly.");
        }
        finally
        {
            await application.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    /// <summary>
    /// Forwards Kestrel's own log records into the service logger, so there is one log destination.
    /// </summary>
    private sealed class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public ForwardingLoggerProvider(ILogger logger) => _logger = logger;

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose()
        {
        }
    }
}
