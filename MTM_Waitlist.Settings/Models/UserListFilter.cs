using System.Text.Json;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The user list's saved filter: the search text and the chosen role code, as one small payload (FR-089).
/// </summary>
/// <remarks>
/// <para>
/// <b>One payload, so a half-saved filter cannot exist.</b> The two halves are written and read together as a
/// single JSON object, which is the difference between "a filter that could not be read" and "half of a filter
/// that was": a reader that could not parse this gets <see cref="FilterReadOutcome.Unreadable"/> and falls back to
/// everyone with the failure stated, rather than applying one half of somebody's filter.
/// </para>
/// <para>
/// It is stored through the existing settings preference path under the key this feature pins, as a
/// <c>text</c> value scoped to the person, and it writes no history row because a preference is not a change to a
/// permission.
/// </para>
/// </remarks>
public sealed record UserListFilter(string SearchText, string RoleCode)
{
    /// <summary>The settings key the filter is stored under. Pinned by the database contract.</summary>
    public const string SettingKey = "admin.users.list_filter";

    /// <summary>No filter at all: everyone, with no search term.</summary>
    public static UserListFilter None { get; } = new(string.Empty, string.Empty);

    /// <summary>Whether this filter hides anybody. A filter that hides nobody is not applied and is not named.</summary>
    public bool IsApplied =>
        !string.IsNullOrWhiteSpace(SearchText) || !string.IsNullOrWhiteSpace(RoleCode);

    /// <summary>The payload as it is stored.</summary>
    public string ToStoredValue() => JsonSerializer.Serialize(new Payload(SearchText, RoleCode));

    /// <summary>
    /// Reads a stored payload.
    /// </summary>
    /// <param name="stored">The stored text, or <see langword="null"/> when nothing was ever saved.</param>
    /// <param name="filter">The filter, or <see cref="None"/> when there is nothing usable to read.</param>
    /// <returns>
    /// <see cref="FilterReadOutcome.None"/> when nothing was saved — an ordinary first visit, not a failure —
    /// <see cref="FilterReadOutcome.Read"/> when a filter was read, and <see cref="FilterReadOutcome.Unreadable"/>
    /// when something was saved that cannot be read, which the page states rather than ignoring.
    /// </returns>
    public static FilterReadOutcome TryRead(string? stored, out UserListFilter filter)
    {
        filter = None;

        if (string.IsNullOrWhiteSpace(stored))
        {
            return FilterReadOutcome.None;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(stored);
            if (payload is null)
            {
                return FilterReadOutcome.Unreadable;
            }

            filter = new UserListFilter(payload.Search?.Trim() ?? string.Empty, payload.RoleCode?.Trim() ?? string.Empty);
            return FilterReadOutcome.Read;
        }
        catch (JsonException)
        {
            return FilterReadOutcome.Unreadable;
        }
    }

    /// <summary>What reading a stored filter produced.</summary>
    public enum FilterReadOutcome
    {
        /// <summary>Nothing was ever saved.</summary>
        None,

        /// <summary>A filter was read.</summary>
        Read,

        /// <summary>Something was saved that cannot be read.</summary>
        Unreadable,
    }

    /// <summary>The stored shape. Both halves travel together or not at all.</summary>
    private sealed record Payload(string? Search, string? RoleCode);
}
