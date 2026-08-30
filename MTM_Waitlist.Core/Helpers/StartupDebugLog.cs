using System.Diagnostics;

using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class StartupDebugLog
{
    private static IStartupLogService? _startupLogService;

    public static void Configure(IStartupLogService? startupLogService)
    {
        _startupLogService = startupLogService;
    }

    [Conditional("DEBUG")]
    public static void Info(string area, string message)
    {
        // The leading bracketed tag is the source (file/control) that owns the log.
        Debug.WriteLine($"[{area}][{DateTimeOffset.Now:O}] {message}");
        _startupLogService?.Info(area, message);
    }

    [Conditional("DEBUG")]
    public static void Error(string area, Exception exception, string message)
    {
        Debug.WriteLine($"[{area}][{DateTimeOffset.Now:O}] ERROR: {message}");
        Debug.WriteLine(exception?.ToString());
        _startupLogService?.Error(area, exception, message);
    }
}
