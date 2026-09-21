using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Turns the one typed answer a user-management write gives into the sentence a screen shows (FR-026, FR-112).
/// </summary>
/// <remarks>
/// The answer already carries its resource key and the shipped English sentence. This resolves the key and uses
/// the shipped sentence only when the resource map holds no entry for it, which is the same fallback shape every
/// other settings screen uses: no resource key is ever shown to a person.
/// </remarks>
public static class UserManagementResults
{
    /// <summary>The sentence to show for <paramref name="result"/>.</summary>
    public static string MessageFor(UserManagementResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.MessageKey))
        {
            return result.Message;
        }

        var localized = result.MessageKey.GetLocalized();

        return string.Equals(localized, result.MessageKey, StringComparison.Ordinal)
            ? result.Message
            : localized;
    }
}
