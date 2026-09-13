using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// The two lines every waitlist card reads.
/// <para>
/// <b>Line 1</b> is the Item's umbrella phrase and <b>Line 2</b> is the Item's identifier. Both are resolved
/// from the Item the request was raised with, through the catalog and
/// <see cref="RequestItemLine2Resolver"/> — so the card reads <c>category</c> + <c>item</c> and nothing else
/// (<c>contracts/card-and-identifier.md</c> §2–§3, FR-005).
/// </para>
/// <para>
/// There is deliberately <b>no legacy fallback</b>: the retired type/subtype pair is not read here, not stored
/// on the request, and not consulted when an Item cannot be resolved (FR-003, FR-023). An Item the catalog does
/// not describe yields an empty first line and the resolver's configuration report on the second, never an
/// invented phrase.
/// </para>
/// </summary>
public static class WaitlistRequestTitles
{
    private static readonly RequestItemLine2Resolver Resolver = new();

    /// <summary>
    /// Card Line 1 — the umbrella phrase: the Category's own word, or the Item's own phrase where the Item
    /// defines one (<c>contracts/card-and-identifier.md</c> §2). Empty when the stored Item code is not
    /// catalogued; a missing code is reported through Line 2 rather than papered over here.
    /// </summary>
    public static string ResolveLine1(RequestItemDefinition? item) => item?.UmbrellaVerb ?? string.Empty;

    /// <summary>
    /// Card Line 2 — the identifier, resolved from the Item's <see cref="RequestItemDefinition.CardLine2Template"/>
    /// against the job snapshot and the captured answer (<c>contracts/card-and-identifier.md</c> §3). An
    /// unresolvable token renders the Item's own display name and reports the configuration problem: never a
    /// blank, never a fabricated value (FR-026).
    /// </summary>
    public static RequestItemLine2Result ResolveLine2(RequestItemDefinition? item, RequestItemLine2Context context)
        => item is null
            ? new RequestItemLine2Result(
                string.Empty,
                false,
                RequestItemLine2Resolver.ProblemKey,
                RequestItemLine2Resolver.ResolveProblemMessage(null!, null))
            : Resolver.Resolve(item, context ?? new RequestItemLine2Context());
}
