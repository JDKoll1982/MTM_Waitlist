using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Mock.Service.ViewModels;

/// <summary>
/// One collapsible settings card on the service settings surface.
/// </summary>
/// <remarks>
/// <para>
/// A group is a set of settings that belong together — the refresh cadence, the API address, the Infor
/// Visual source, and so on. It carries its own heading and its own explanatory note, which is the shape
/// the main app's settings window uses: one collapsible card per topic, each setting inside it as a card
/// of its own.
/// </para>
/// <para>
/// The group also carries its own search state, so the title-bar search box can hide the groups that do
/// not match and open the ones that do without the page needing to know how many groups there are.
/// </para>
/// </remarks>
public sealed class ServiceSettingsGroupViewModel : ObservableObject
{
    private readonly string[] _keywords;

    private bool _isVisible = true;
    private bool _isExpanded;

    /// <summary>Creates a group.</summary>
    /// <param name="key">Stable identifier, used for diagnostics and tests.</param>
    /// <param name="headerText">The heading shown on the collapsible card.</param>
    /// <param name="descriptionText">The note shown under the heading.</param>
    /// <param name="keywords">
    /// Extra words that should find this group: the words an operator would plausibly type, including the
    /// technical names of the settings inside it. The heading and the note are matched as well, so a
    /// keyword list only has to add what is not already visible.
    /// </param>
    public ServiceSettingsGroupViewModel(string key, string headerText, string descriptionText, params string[] keywords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Key = key;
        HeaderText = headerText ?? string.Empty;
        DescriptionText = descriptionText ?? string.Empty;
        _keywords = keywords ?? [];
    }

    /// <summary>Stable identifier for the group.</summary>
    public string Key { get; }

    /// <summary>The group's heading.</summary>
    public string HeaderText { get; }

    /// <summary>The note that says what the group covers.</summary>
    public string DescriptionText { get; }

    /// <summary>Whether the group matches the current search.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    /// <summary>Whether the collapsible card is open. A search opens the cards it matched.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Applies a search: the group stays visible only when it matches, and an active search opens it so
    /// the operator sees what matched instead of a closed card.
    /// </summary>
    /// <param name="query">The text in the search box.</param>
    public void ApplySearch(string? query)
    {
        var isSearching = !string.IsNullOrWhiteSpace(query);
        var matches = Matches(query);

        IsVisible = matches;

        if (!isSearching)
        {
            return;
        }

        IsExpanded = matches;
    }

    /// <summary>Whether this group matches a query.</summary>
    /// <param name="query">The text in the search box.</param>
    /// <returns><see langword="true"/> when the group is empty, the query is empty, or every word matches.</returns>
    public bool Matches(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        // Every word must match something, so "mysql port" narrows rather than widens: a word may match the
        // heading, the note, or any keyword.
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.All(MatchesWord);
    }

    private bool MatchesWord(string word)
    {
        if (Contains(HeaderText, word) || Contains(DescriptionText, word) || Contains(Key, word))
        {
            return true;
        }

        return _keywords.Any(keyword => Contains(keyword, word));
    }

    private static bool Contains(string haystack, string word) =>
        haystack.Contains(word, StringComparison.CurrentCultureIgnoreCase);
}
