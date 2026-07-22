using System.Diagnostics;

namespace MTM_Waitlist.Helpers;

public static class StartupDebugLog
{
    [Conditional("DEBUG")]
    public static void Info(string area, string message)
    {
        Debug.WriteLine($"[STARTUP][{DateTimeOffset.Now:O}][{area}] {message}");
    }

    [Conditional("DEBUG")]
    public static void Error(string area, Exception exception, string message)
    {
        Debug.WriteLine($"[STARTUP][{DateTimeOffset.Now:O}][{area}] ERROR: {message}");
        Debug.WriteLine(exception.ToString());
    }
}
