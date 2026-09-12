using Microsoft.Extensions.Logging;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Routes every <see cref="ILogger"/> message the service's container produces into
/// <see cref="ServiceLog"/>, so the refresh engine, backup pipeline, API host and settings surfaces all
/// leave a durable record on the host (T148(c)).
/// </summary>
/// <remarks>
/// The default logging registration in <see cref="ServiceHostBuilder.Build"/> had no file sink, so the only
/// record of a failure was the attached debugger's output — which is nothing on a machine nobody is sitting
/// at. This provider is the sink.
/// </remarks>
internal sealed class ServiceFileLoggerProvider : ILoggerProvider
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new ServiceFileLogger(categoryName);

    /// <inheritdoc />
    /// <remarks>Nothing is held open: <see cref="ServiceLog"/> appends per line.</remarks>
    public void Dispose()
    {
    }

    /// <summary>Writes each formatted message to the durable daily file.</summary>
    private sealed class ServiceFileLogger : ILogger
    {
        private readonly string _category;

        /// <summary>Creates the logger for one category.</summary>
        /// <param name="category">The category name, recorded as the log area.</param>
        internal ServiceFileLogger(string category) =>
            _category = string.IsNullOrWhiteSpace(category) ? nameof(ServiceFileLogger) : category;

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <inheritdoc />
        /// <remarks>
        /// The formatted message and the exception's full text are recorded. The state object's properties
        /// are deliberately not enumerated: a structured property could carry a connection string, and
        /// FR-026 forbids credential material reaching a durable store.
        /// </remarks>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel))
            {
                return;
            }

            ServiceLog.Write(
                ToLevelText(logLevel),
                _category,
                formatter(state, exception),
                exception?.ToString());
        }

        private static string ToLevelText(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => "INFO"
        };
    }
}
