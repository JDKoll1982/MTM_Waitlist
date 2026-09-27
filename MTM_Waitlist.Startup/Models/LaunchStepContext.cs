using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What a step is handed when it runs (`contracts/launch-step-contract.md` §2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The person arrives only after identity has been resolved.</b> <see cref="Person"/> is <c>null</c> for the
/// steps that run before sign-in, so a pre-sign-in step cannot read a person that has not been established yet
/// and cannot attribute work to one.
/// </para>
/// <para>
/// <b>The machine's facts are read-only and always available.</b> <see cref="Machine"/> is the same read-only
/// contract every other consumer sees (FR-022), so a step reads the machine rather than changing it.
/// </para>
/// <para>
/// <b>A step never reads the store itself.</b> It calls a service and that service calls a stored procedure, so
/// the data-access rule holds through the launch as it does everywhere else. The feed is on the context so a
/// step can add its own sub-operation lines (FR-002).
/// </para>
/// </remarks>
/// <param name="Person">The signed-in person, or <c>null</c> before identity has been resolved.</param>
/// <param name="Machine">This computer's facts, for reading only.</param>
/// <param name="Feed">The activity feed the step writes its own lines to.</param>
public sealed record LaunchStepContext(
    IPersonIdentity? Person,
    IMachineFacts Machine,
    ILaunchActivityFeed Feed);
