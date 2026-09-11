namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// A surface the shell's title-bar search box can search: the status surface and the settings surface
/// each answer with their own suggestions and apply the query to what they show.
/// </summary>
/// <remarks>
/// The shell owns the search box (it lives in the window's title bar, so it outlives the page in the
/// content frame), and the active page owns what a search means. That split is what the main app's shell
/// does with its own title-bar search box: the shell asks the current page, the page filters its rows.
/// </remarks>
public interface IServiceSearchTarget
{
    /// <summary>
    /// The text the search box holds for this surface. The shell reads it when a surface is shown so the
    /// box and the surface agree, including when an earlier search is still narrowing what is displayed.
    /// </summary>
    string SearchQuery { get; }

    /// <summary>The suggestions currently offered for the text the operator has typed.</summary>
    IReadOnlyList<string> SearchSuggestions { get; }

    /// <summary>
    /// Applies the text as it is typed: refreshes the suggestions and narrows what the surface shows.
    /// </summary>
    /// <param name="query">The text in the search box.</param>
    void UpdateSearchSuggestions(string query);

    /// <summary>
    /// Applies an accepted search — the operator pressed Enter or picked a suggestion.
    /// </summary>
    /// <param name="query">The text in the search box.</param>
    /// <param name="chosenSuggestion">The suggestion that was picked, when one was.</param>
    void SubmitSearch(string query, string? chosenSuggestion);
}
