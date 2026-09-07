using System.Web;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The deep-link argument contract used by the file-09 new-request alert toast. The submit/toast path builds an
/// argument string (carried on the toast's <c>launch</c> attribute) that uniquely identifies a created request; the
/// activation handler parses it back and navigates to that request's detail page. Pure + unit-testable.
/// </summary>
public static class WaitlistRequestLink
{
    /// <summary>Action value meaning "open a specific waitlist request".</summary>
    public const string ActionOpenRequest = "openrequest";

    public const string ActionKey = "action";

    public const string RequestKey = "request";

    /// <summary>
    /// Builds the toast argument string for opening a request, e.g.
    /// <c>action=openrequest&amp;request=&lt;guid&gt;</c>.
    /// </summary>
    public static string Build(Guid requestId)
        => $"action={ActionOpenRequest}&{RequestKey}={requestId:D}";

    /// <summary>
    /// Parses a toast argument string. Returns true and sets <paramref name="requestId"/> when the arguments name
    /// the <c>openrequest</c> action with a valid request guid.
    /// </summary>
    public static bool TryParse(string? arguments, out Guid requestId)
    {
        requestId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return false;
        }

        var parsed = HttpUtility.ParseQueryString(arguments);
        if (!string.Equals(parsed[ActionKey], ActionOpenRequest, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var raw = parsed[RequestKey];
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out requestId);
    }
}
