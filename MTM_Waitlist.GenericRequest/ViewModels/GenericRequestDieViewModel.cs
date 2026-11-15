using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The die step: a search of Infor Visual, then the die or dies the request needs.
/// </summary>
/// <remarks>
/// <para>
/// A generic request has no job to read a die from, so this step is the search itself (020 FR-001). It asks the seam
/// one question and the answer says which of the three presentations to draw, so a term that matched nothing is told
/// apart from a source that could not be asked without the screen guessing at either.
/// </para>
/// <para>
/// A tap marks a die rather than leaving the step: a request may need several, and each one becomes its own request
/// (FR-010). A die already marked stays marked when a later search returns it, so narrowing a search never quietly
/// drops a choice.
/// </para>
/// </remarks>
public sealed partial class GenericRequestDieViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    /// <summary>Every die the requester has marked, in the order they marked them.</summary>
    private readonly List<GenericRequestDieOption> _chosen = new();

    public GenericRequestDieViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
        SearchCommand = new AsyncRelayCommand(LoadAsync);
    }

    public override GenericRequestStep Step => GenericRequestStep.Die;

    public override string TitleKey => "GenericRequest_Die_Title.Text";

    public override string IntroKey => "GenericRequest_Die_Intro.Text";

    public override string EmptyKey => "GenericRequest_Die_Empty.Text";

    /// <summary>Run the search again with the text as typed.</summary>
    public IAsyncRelayCommand SearchCommand { get; }

    /// <summary>The dies this search matched, each one a choice.</summary>
    public ObservableCollection<GenericRequestDieOption> Options { get; } = new();

    /// <summary>What the requester typed, which a failed read keeps so the retry asks the same question.</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>How many dies this search matched, drawn above the list so a partial list cannot pass for a full one.</summary>
    [ObservableProperty]
    public partial int MatchCount { get; set; }

    /// <summary>The match count as the step draws it, or empty when there is nothing to count.</summary>
    public string CountText => MatchCount switch
    {
        0 => string.Empty,
        1 => "GenericRequest_Die_Count_One.Text".GetLocalized(),
        _ => string.Format(
            CultureInfo.CurrentCulture,
            "GenericRequest_Die_Count.Text".GetLocalized(),
            MatchCount),
    };

    /// <summary>Whether there is a count to draw at all.</summary>
    public bool HasCount => MatchCount > 0;

    /// <summary>
    /// The line the step shows while nothing is marked, so a Continue that cannot be pressed has its reason beside
    /// it rather than looking broken.
    /// </summary>
    public string HintText => IsPopulated && _chosen.Count == 0
        ? "GenericRequest_Die_NothingChosen.Text".GetLocalized()
        : string.Empty;

    /// <summary>Whether the step has a hint to draw.</summary>
    public bool HasHint => HintText.Length > 0;

    protected override bool CanAnswer => _chosen.Count > 0;

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadDiesAsync(SearchText);

        foreach (var previous in Options)
        {
            previous.PropertyChanged -= OnOptionChanged;
        }

        Options.Clear();

        foreach (var option in result.Items)
        {
            option.IsSelected = IsChosen(option.DieNumber);
            option.PropertyChanged += OnOptionChanged;
            Options.Add(option);
        }

        AdoptFreshRows();

        MatchCount = result.Count;
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasCount));

        Apply(result);
        NotifyChoiceChanged();
    }

    protected override void CommitToDraft()
    {
        Draft.SelectedDies.Clear();
        Draft.SelectedDies.AddRange(_chosen);
        Draft.DieNumber = _chosen.Count == 0 ? string.Empty : _chosen[0].DieNumber;
    }

    /// <summary>
    /// Replace a marked die with the row this search returned for it, so the request records the die's current
    /// description and location rather than a copy from an earlier search.
    /// </summary>
    private void AdoptFreshRows()
    {
        for (var index = 0; index < _chosen.Count; index++)
        {
            var fresh = Options.FirstOrDefault(option => SameDie(option.DieNumber, _chosen[index].DieNumber));
            if (fresh is not null)
            {
                _chosen[index] = fresh;
            }
        }
    }

    private bool IsChosen(string dieNumber) => _chosen.Any(chosen => SameDie(chosen.DieNumber, dieNumber));

    private void OnOptionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is not GenericRequestDieOption option
            || !string.Equals(args.PropertyName, nameof(GenericRequestDieOption.IsSelected), StringComparison.Ordinal))
        {
            return;
        }

        _chosen.RemoveAll(chosen => SameDie(chosen.DieNumber, option.DieNumber));

        if (option.IsSelected)
        {
            _chosen.Add(option);
        }

        NotifyChoiceChanged();
    }

    private void NotifyChoiceChanged()
    {
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(HasHint));
        NotifyAnswerChanged();
    }

    private static bool SameDie(string first, string second) =>
        string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
}
