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
/// module filter works for both surfaces without either surface supplying the module. A category can be longer
/// than the column that holds it, so what is handed on is bounded — see <see cref="MaximumModuleLength"/>.
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
    /// <summary>
    /// The longest module this provider hands the store, which is the log table's own <c>module</c> width.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A category really can be longer than the column, and this bound is here because one was.</b> A category
    /// is a namespace-qualified type name and the longest in this tree is 77 characters. The column held 64, so
    /// on 2026-09-28 every entry from <c>ImageStorageConfigurationResolver</c> was refused by the store with
    /// "Data too long for column 'p_module'" and none of them was recorded at all.
    /// </para>
    /// <para>
    /// <b>The bound keeps the tail rather than the head, and marks the cut.</b> The leaf type name is what a
    /// reader filters by and the namespace prefix is what they can do without, so a category that does not fit is
    /// cut from the front and prefixed with an ellipsis rather than losing its most informative end.
    /// </para>
    /// <para>
    /// <b>It is bounded here rather than at the writer.</b> The module is part of the entry, and the entry's
    /// <c>error_fingerprint</c> is computed over it: a value shortened on its way to the store would leave a row
    /// whose fingerprint could not be recomputed from the row itself, which is what a fingerprint is for.
    /// </para>
    /// </remarks>
    public const int MaximumModuleLength = 128;

    private readonly LogService _logService;

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="logService">
    /// The seam to write through. The concrete seam rather than its interface, because this provider is part of
    /// the logging module and is the one caller that has a fault plus the structured properties to hand over
    /// together.
    /// </param>
    /// <remarks>
    /// The threshold is taken from the seam rather than passed in beside it, so the process holds one threshold
    /// and the two doors onto the store cannot disagree about what is worth keeping.
    /// </remarks>
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
            _module = Bounded(string.IsNullOrWhiteSpace(categoryName) ? "Application" : categoryName);
            _logService = logService;
        }

        /// <summary>
        /// Cuts a module to what the store holds, keeping its tail and saying that the front was left off.
        /// </summary>
        /// <param name="module">The category name, which is never already blank.</param>
        /// <returns>The module itself when it fits, otherwise its tail behind an ellipsis.</returns>
        private static string Bounded(string module)
            => module.Length <= MaximumModuleLength
                ? module
                : "…" + module[^(MaximumModuleLength - 1)..];

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
        /// Everything at or above the store's threshold is enabled, and everything below it is not: a level the
        /// store would drop is not worth the caller building its message and structured properties for. The store
        /// is still the filter a reader narrows with — the panel queries by severity — and the threshold is a
        /// deployment's decision (<see cref="LogStoreLevel.SectionName"/>), defaulting to warnings and faults.
        /// </remarks>
        public bool IsEnabled(LogLevel logLevel) => _logService.Level.Keeps(logLevel);

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
                    LogStoreLevel.SeverityOf(logLevel),
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
