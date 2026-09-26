using Microsoft.Extensions.Logging;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// Writes <c>ILogger</c> output to the same store, through the same seam (`contracts/logging-contract.md` §4,
/// plan D5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a provider and not an edit.</b> The application already has hundreds of <c>ILogger</c> call sites
/// across dozens of files. The abstraction is sound and only lacked a destination, so the migration is one
/// registration rather than one edit per call site — and it is what keeps the structured properties those call
/// sites already carry, which rewriting them by hand would have flattened into the message.
/// </para>
/// <para>
/// <b>The category name becomes the module.</b> A call site's category is the type that logged, so the panel's
/// module filter works for both surfaces without either surface supplying the module.
/// </para>
/// <para>
/// <b>This provider serves this application's own host only.</b> The separate on-host service that refreshes the
/// external-read cache runs its own container with its own file logger, and it is not served by this provider.
/// </para>
/// <para>
/// <b>A logging call site is never broken by logging.</b> Every member absorbs its own failure: a caller that
/// logged must continue exactly as it would have.
/// </para>
/// </remarks>
public sealed class StoreLoggerProvider : ILoggerProvider
{
    private readonly LogService _logService;

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="logService">
    /// The seam to write through. The concrete seam rather than its interface, because this provider is part of
    /// the logging module and is the one caller that has a fault plus the structured properties to hand over
    /// together.
    /// </param>
    public StoreLoggerProvider(LogService logService)
    {
        ArgumentNullException.ThrowIfNull(logService);
        _logService = logService;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new StoreLogger(categoryName, _logService);

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing is owned here: the queue belongs to the writer, which the host stops.
    }

    /// <summary>
    /// One category's logger. It holds the category, which is the entry's module, and the seam.
    /// </summary>
    private sealed class StoreLogger : ILogger
    {
        private readonly string _module;
        private readonly LogService _logService;

        internal StoreLogger(string categoryName, LogService logService)
        {
            _module = string.IsNullOrWhiteSpace(categoryName) ? "Application" : categoryName;
            _logService = logService;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Scopes are not carried into the entry: the seam already generates a correlation identifier for an
        /// entry whose caller named none, and a scope's ambient state would have to be surfaced as a second
        /// correlation source the panel cannot tell apart.
        /// </remarks>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NoScope.Instance;

        /// <inheritdoc />
        /// <remarks>
        /// Everything but <see cref="LogLevel.None"/> is enabled, because the store is the filter: the panel
        /// narrows by severity when it is read, and a level the store never received cannot be recovered later.
        /// </remarks>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                var message = formatter is null ? string.Empty : formatter(state, exception) ?? string.Empty;

                var entry = new LogEntry(
                    SeverityFor(logLevel),
                    _module,
                    message,
                    OutcomeFor(logLevel),
                    Blank(eventId.Name) ? null : eventId.Name,
                    exception?.GetType().FullName,
                    ExceptionDetail: null,
                    ExceptionFingerprint: null,
                    Target: null,
                    CorrelationId: null);

                _logService.Capture(entry, exception, Properties(state));
            }
            catch (Exception)
            {
                // A logging call site must never be the reason a caller fails, which is the same rule the seam
                // follows one level down (FR-037).
            }
        }

        /// <summary>
        /// Projects the structured properties a call site already carries, so they land in the entry's
        /// <c>payload_json</c> rather than being flattened into its message (contract §4).
        /// </summary>
        private static IReadOnlyDictionary<string, object?>? Properties<TState>(TState state)
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values || values.Count == 0)
            {
                return null;
            }

            var properties = new Dictionary<string, object?>(values.Count, StringComparer.Ordinal);

            foreach (var (name, value) in values)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    properties[name] = value;
                }
            }

            return properties.Count == 0 ? null : properties;
        }

        private static LogSeverity SeverityFor(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => LogSeverity.Debug,
            LogLevel.Information => LogSeverity.Info,
            LogLevel.Warning => LogSeverity.Warning,
            LogLevel.Error => LogSeverity.Error,
            LogLevel.Critical => LogSeverity.Critical,
            _ => LogSeverity.Info,
        };

        private static string OutcomeFor(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Warning => "Blocked",
            LogLevel.Error or LogLevel.Critical => "Failure",
            _ => "Success",
        };

        private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

        /// <summary>The no-op scope, so a caller's <c>using</c> block costs nothing.</summary>
        private sealed class NoScope : IDisposable
        {
            internal static readonly NoScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
