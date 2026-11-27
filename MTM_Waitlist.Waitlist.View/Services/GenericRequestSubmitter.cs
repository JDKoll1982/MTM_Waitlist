using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Raises a generic request through the queue's one write door.
/// </summary>
/// <remarks>
/// <para>
/// It lives here, beside <see cref="IWaitlistRequestService"/>, because turning a generic request into a queue draft is
/// knowledge the waitlist module owns and nowhere else has (024-dependency-modernization, MOD-007). The generic
/// request library knows only <see cref="IGenericRequestSubmitter"/>, so the dependency runs one way.
/// </para>
/// <para>
/// The mapping is the whole of this class, and it is the place a field goes missing: every value the flow captured is
/// carried across, the request is always generic, and the due time is deliberately left unset so the store derives it
/// from the request's own creation time and the allotment its request type configures.
/// </para>
/// </remarks>
public sealed class GenericRequestSubmitter : IGenericRequestSubmitter
{
    private readonly IWaitlistRequestService _requests;

    public GenericRequestSubmitter(IWaitlistRequestService requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        _requests = requests;
    }

    /// <inheritdoc />
    public async Task<GenericRequestSubmitResult> SubmitAsync(
        GenericRequestSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _requests
            .SubmitAsync(
                new WaitlistRequestDraft
                {
                    Building = request.Building,
                    WorkCenter = request.WorkCenter,
                    Category = request.Category,
                    Item = request.Item,
                    InputValue = request.InputValue,
                    Answers = request.Answers,
                    // A generic request has no job and no work order anywhere; the flag is what says so on purpose,
                    // rather than a blank standing in for a value that went missing.
                    IsGeneric = true,
                    ProvenanceWorkOrder = request.ProvenanceWorkOrder,
                    Description = request.Description,
                    AcceptedWarnings = request.AcceptedWarnings,
                    WorkCenterName = request.WorkCenterName,
                    RequesterEmployeeNumber = request.RequesterEmployeeNumber,
                    RequesterEmployeeName = request.RequesterEmployeeName,
                    RequestedUtc = request.RequestedUtc,
                    IsOverdue = request.IsOverdue,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Status == WaitlistRequestSubmitStatus.Success && result.Request is { } raised)
        {
            return GenericRequestSubmitResult.Raised(raised.Id);
        }

        if (result.Status == WaitlistRequestSubmitStatus.DuplicateWarningRequired)
        {
            return GenericRequestSubmitResult.DuplicateWarning(result.Message);
        }

        return result.Status == WaitlistRequestSubmitStatus.ValidationFailure
            ? GenericRequestSubmitResult.ValidationFailure(result.Message)
            : GenericRequestSubmitResult.PersistenceFailure(result.Message);
    }
}
