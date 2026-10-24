namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One thing the settings hub's search can find: the setting's name, the page it lives on, and the section that
/// has to be opened and scrolled to so the reader lands on it rather than on the top of a page.
/// </summary>
/// <param name="Title">What the search result is called.</param>
/// <param name="Area">The page's name, shown beside the title so two similar settings are told apart.</param>
/// <param name="PageRoute">The page-service route key of the page that holds it.</param>
/// <param name="SectionKey">
/// The <c>x:Name</c> or automation id of the section on that page. The page opens any collapsed section around it
/// and scrolls it into view.
/// </param>
/// <param name="Keywords">The words a person is likely to type, including the ones the title does not use.</param>
public sealed record SettingsSearchTarget(
    string Title,
    string Area,
    string PageRoute,
    string SectionKey,
    string Keywords)
{
    /// <summary>Whether this entry answers <paramref name="query"/>.</summary>
    /// <remarks>
    /// Every word of the query has to appear somewhere in the entry, so "work centre" finds the work-centre section
    /// and "picture folder" finds the folder section. Comparing per word rather than as one substring is what makes
    /// two-word searches work at all.
    /// </remarks>
    public bool Matches(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var haystack = $"{Title} {Area} {Keywords}";
        foreach (var word in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!haystack.Contains(word, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
