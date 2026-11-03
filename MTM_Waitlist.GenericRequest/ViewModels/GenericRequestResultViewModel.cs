using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The result step: what happened when the request was raised.
/// </summary>
/// <remarks>
/// The presentation comes from the raise attempt the summary recorded on the draft, so this step reports what the
/// store actually answered rather than what the flow hoped for: a raised request shows its number, a refused one
/// shows the store's own words with the attempt time and a retry, and a flow nobody finished reads empty.
/// </remarks>
public sealed partial class GenericRequestResultViewModel : GenericRequestStepViewModel
{
    public GenericRequestResultViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);
    }

    public override GenericRequestStep Step => GenericRequestStep.Result;

    public override string TitleKey => "GenericRequest_Result_Title.Text";

    public override string IntroKey => "GenericRequest_Result_Intro.Text";

    public override string EmptyKey => "GenericRequest_Result_Empty.Text";

    /// <summary>The raised request's identifier, or empty when nothing was raised.</summary>
    public string RequestNumber => Draft.RaisedRequestNumber;

    /// <summary>The store's own explanation of a refusal, or empty when the request was raised.</summary>
    public string ResultMessage => Draft.ResultMessage;

    /// <summary>Whether the refusal note is drawn.</summary>
    public Visibility FailureVisibility => Draft.ResultPresentation == GenericRequestPresentation.Unavailable
        ? Visibility.Visible
        : Visibility.Collapsed;

    /// <summary>Whether the raised request's number is drawn.</summary>
    public Visibility NumberVisibility => Draft.ResultPresentation == GenericRequestPresentation.Populated
        ? Visibility.Visible
        : Visibility.Collapsed;

    protected override Task LoadCoreAsync()
    {
        var attemptedAt = Draft.ResultAttemptedAtUtc ?? DateTimeOffset.UtcNow;

        Apply(Draft.ResultPresentation switch
        {
            GenericRequestPresentation.Populated => GenericRequestStepAvailability.Ready(attemptedAt),
            GenericRequestPresentation.Unavailable => GenericRequestStepAvailability.Unavailable(attemptedAt),
            _ => GenericRequestStepAvailability.Empty(attemptedAt),
        });

        OnPropertyChanged(nameof(RequestNumber));
        OnPropertyChanged(nameof(ResultMessage));
        OnPropertyChanged(nameof(FailureVisibility));
        OnPropertyChanged(nameof(NumberVisibility));

        return Task.CompletedTask;
    }
}
