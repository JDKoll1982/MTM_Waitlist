using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// The one door a generic request raises through.
/// </summary>
/// <remarks>
/// <para>
/// The flow describes the request it wants raised and reads what the queue answered; it does not know which service
/// holds the queue (024-dependency-modernization, MOD-007). The implementation lives in the waitlist module, beside
/// the write door it wraps, so the dependency runs one way: this library knows the contract, and the module that owns
/// the queue knows the contract, and neither knows the other.
/// </para>
/// <para>
/// The write is live, always. It goes through the same stored procedure every other request uses, and a duplicate is
/// refused by the store rather than checked here.
/// </para>
/// </remarks>
public interface IGenericRequestSubmitter
{
    /// <summary>
    /// Raises the described request, or reports in plain language why it was not raised.
    /// </summary>
    /// <param name="request">The request to raise.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<GenericRequestSubmitResult> SubmitAsync(
        GenericRequestSubmitRequest request,
        CancellationToken cancellationToken = default);
}
