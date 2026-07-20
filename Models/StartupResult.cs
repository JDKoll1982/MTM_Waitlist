namespace MTM_Waitlist.Models;

public sealed class StartupResult
{
    public bool IsSuccess { get; init; }

    public bool IsBlocked { get; init; }

    public string RouteTarget { get; init; } = string.Empty;

    public string StatusMessage { get; init; } = string.Empty;

    public static StartupResult Success(string routeTarget, string statusMessage = "Startup complete")
    {
        return new StartupResult
        {
            IsSuccess = true,
            IsBlocked = false,
            RouteTarget = routeTarget,
            StatusMessage = statusMessage
        };
    }

    public static StartupResult Blocked(string statusMessage)
    {
        return new StartupResult
        {
            IsSuccess = false,
            IsBlocked = true,
            RouteTarget = string.Empty,
            StatusMessage = statusMessage
        };
    }
}
