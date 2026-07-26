using System.Diagnostics;

using MTM_Waitlist.Contracts.Services;

namespace MTM_Waitlist.Helpers;

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
        Debug.WriteLine($"[STARTUP][{DateTimeOffset.Now:O}][{area}] {message}");
        _startupLogService?.Info(area, message);
    }

    [Conditional("DEBUG")]
    public static void Error(string area, Exception exception, string message)
    {
        Debug.WriteLine($"[STARTUP][{DateTimeOffset.Now:O}][{area}] ERROR: {message}");
        Debug.WriteLine(exception?.ToString());
        _startupLogService?.Error(area, exception, message);
    }
}
