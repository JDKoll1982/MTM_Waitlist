using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The result step: what happened when the request was raised, and the ways out of it.
/// </summary>
/// <remarks>
/// <para>
/// It is the same screen the work-order flow ends on — one icon, one heading, one sentence, and the buttons that fit
/// the outcome — because both flows raise the same kind of request and a person should not have to learn a second
/// ending. The three outcomes are success, a duplicate the queue refused, and a failure.
/// </para>
/// <para>
/// Which one is drawn comes from the raise attempt the review recorded on the draft, so the screen reports what the
/// store answered rather than what the flow hoped for: a raised request shows the number the store gave it, a refusal
/// shows the queue's own words, and a failure offers a retry that asks again rather than one that only redraws.
/// </para>
/// </remarks>
public sealed partial class GenericRequestResultViewModel : GenericRequestStepViewModel
{
    private const string SuccessGlyph = "\uE73E";
    private const string DuplicateGlyph = "\uE7BA";
    private const string FailureGlyph = "\uEA39";

    private readonly IGenericRequestStepData _data;

    public GenericRequestResultViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    /// <inheritdoc />
    public override GenericRequestStep Step => GenericRequestStep.Result;

    /// <inheritdoc />
    public override string TitleKey => "GenericRequest_Result_Title.Text";

    /// <inheritdoc />
    public override string IntroKey => "GenericRequest_Result_Intro.Text";

    /// <inheritdoc />
    public override string EmptyKey => "GenericRequest_Result_Empty.Text";

    /// <summary>The outcome's heading.</summary>
    [ObservableProperty]
    public partial string Heading { get; set; } = string.Empty;

    /// <summary>The Segoe Fluent Icons glyph the outcome draws.</summary>
    [ObservableProperty]
    public partial string IconGlyph { get; set; } = SuccessGlyph;

    /// <summary>The sentence under the heading: the request number, the queue's refusal, or why the write failed.</summary>
    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    /// <summary>Whether the store accepted the request.</summary>
    [ObservableProperty]
    public partial bool IsSuccess { get; set; }

    /// <summary>Whether the queue refused it because an active request for the same thing already exists.</summary>
    [ObservableProperty]
    public partial bool IsDuplicate { get; set; }

    /// <summary>Whether the attempt did not land and can be tried again.</summary>
    [ObservableProperty]
    public partial bool IsFailure { get; set; }

    /// <summary>Whether a retry is in flight.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Begin another request, keeping only the building.</summary>
    [RelayCommand]
    private void AddAnotherRequest() => Navigator.StartAnother();

    /// <summary>Show the waitlist, where the request just raised is waiting.</summary>
    [RelayCommand]
    private void ReturnToWaitlist() => Navigator.LeaveToWaitlist();

    /// <summary>Leave without raising anything.</summary>
    [RelayCommand]
    private void CancelToWaitlist() => Navigator.LeaveToWaitlist();

    /// <summary>Ask the queue to raise the request again.</summary>
    /// <remarks>
    /// The inherited retry control reloads the step, which for a result would only redraw the same failure. A person
    /// pressing Retry here means "try the write again", so this is a resubmit and the page binds it instead.
    /// </remarks>
    [RelayCommand]
    private async Task ResubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var submission = await _data.SubmitAsync(Draft).ConfigureAwait(true);

            Draft.RaisedRequestNumber = submission.RequestId;
            Draft.ResultWasDuplicate = submission.IsDuplicate;
            Draft.ResultMessage = submission.FailureMessage;
            Draft.ResultAttemptedAtUtc = submission.AttemptedAtUtc;
            Draft.ResultPresentation = submission.IsRaised
                ? GenericRequestPresentation.Populated
                : GenericRequestPresentation.Unavailable;

            await LoadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // A cancelled attempt is not a failed one, so it changes nothing on screen.
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "GenericRequestResult",
                ex,
                "Resubmitting a generic request threw an unexpected exception.");

            ApplyOutcome(
                GenericRequestResultOutcome.Failure,
                "GenericRequest_Result_RetryFailed.Text".GetLocalized());
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <inheritdoc />
    protected override Task LoadCoreAsync()
    {
        var attemptedAt = Draft.ResultAttemptedAtUtc ?? DateTimeOffset.UtcNow;

        Apply(Draft.ResultPresentation switch
        {
            GenericRequestPresentation.Populated => GenericRequestStepAvailability.Ready(attemptedAt),
            GenericRequestPresentation.Unavailable => GenericRequestStepAvailability.Unavailable(attemptedAt),
            _ => GenericRequestStepAvailability.Empty(attemptedAt),
        });

        var raised = Draft.ResultPresentation == GenericRequestPresentation.Populated;

        ApplyOutcome(
            raised
                ? GenericRequestResultOutcome.Success
                : Draft.ResultWasDuplicate
                    ? GenericRequestResultOutcome.Duplicate
                    : GenericRequestResultOutcome.Failure,
            raised ? RaisedMessage(Draft.RaisedRequestNumber) : FailureMessage(Draft.ResultMessage));

        return Task.CompletedTask;
    }

    private void ApplyOutcome(GenericRequestResultOutcome outcome, string message)
    {
        IsSuccess = outcome == GenericRequestResultOutcome.Success;
        IsDuplicate = outcome == GenericRequestResultOutcome.Duplicate;
        IsFailure = outcome == GenericRequestResultOutcome.Failure;

        Message = message;

        (Heading, IconGlyph) = outcome switch
        {
            GenericRequestResultOutcome.Success
                => ("GenericRequest_Result_Heading_Success.Text".GetLocalized(), SuccessGlyph),
            GenericRequestResultOutcome.Duplicate
                => ("GenericRequest_Result_Heading_Duplicate.Text".GetLocalized(), DuplicateGlyph),
            _ => ("GenericRequest_Result_Heading_Failure.Text".GetLocalized(), FailureGlyph),
        };
    }

    private static string RaisedMessage(string requestNumber) =>
        string.IsNullOrWhiteSpace(requestNumber)
            ? "GenericRequest_Result_Raised_NoNumber.Text".GetLocalized()
            : string.Format(
                CultureInfo.CurrentCulture,
                "GenericRequest_Result_Raised_Message.Text".GetLocalized(),
                requestNumber);

    /// <summary>
    /// The sentence a refusal or a failure shows, never blank.
    /// </summary>
    /// <remarks>
    /// A draft nobody finished carries no message at all, and a result page with a heading and no sentence reads as a
    /// broken screen. The empty step's own line is the honest answer there, because nothing was raised.
    /// </remarks>
    private string FailureMessage(string message) =>
        string.IsNullOrWhiteSpace(message) ? EmptyKey.GetLocalized() : message;

    private enum GenericRequestResultOutcome
    {
        Success,
        Duplicate,
        Failure,
    }
}
