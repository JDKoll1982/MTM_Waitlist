using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The values a card's Line 2 template resolves against: the active job's fields and the one answer the flow
/// captured. A pure value so the resolver stays deterministic and DB-free.
/// </summary>
/// <remarks>
/// There is no destination here, and its absence is deliberate: a die always goes to the home location the job
/// records, so nothing is asked about where it goes and nothing about the card may be shaped by an answer to that
/// retired question (FR-054, D22).
/// </remarks>
public sealed record RequestItemLine2Context(
    string? PartNumber = null,
    string? PartDescription = null,
    string? DieNumber = null,
    string? DieLocation = null,
    string? DunnagePart = null,
    string? SequenceNumber = null,
    string? ScrapType = null,
    string? Answer = null,
    string? Component = null,
    string? Defect = null,
    string? JobPartNumber = null);

/// <summary>The resolved identifier, and whether it resolved at all.</summary>
public sealed record RequestItemLine2Result(string Text, bool IsResolved, string ProblemKey, string Problem)
{
    /// <summary>A result that resolved cleanly, with nothing to report.</summary>
    public static RequestItemLine2Result Resolved(string text) => new(text, true, string.Empty, string.Empty);
}

/// <summary>
/// Resolves an Item's <see cref="RequestItemDefinition.CardLine2Template"/> — the card's identifier — against
/// the active-job snapshot and the captured answer (contract §3, FR-005).
/// <para>
/// The token set is deliberately closed. An unknown token, or a token whose value is blank, renders the Item's
/// own display name and reports the configuration problem: never a blank, never a fabricated value (FR-026).
/// </para>
/// </summary>
public sealed class RequestItemLine2Resolver
{
    /// <summary>The resource key of the configuration-problem report (FR-022).</summary>
    public const string ProblemKey = "RequestItem_Line2.Problem";

    /// <summary>Resolves the Item's Line 2. Never returns null and never returns a bare template.</summary>
    public RequestItemLine2Result Resolve(RequestItemDefinition item, RequestItemLine2Context context)
    {
        ArgumentNullException.ThrowIfNull(item);
        var effectiveContext = context ?? new RequestItemLine2Context();

        return TryResolveTemplate(
            item.CardLine2Template,
            BuildTokens(effectiveContext),
            out var text,
            out var unresolvedToken)
                ? RequestItemLine2Result.Resolved(text)
                : new RequestItemLine2Result(
                    ResolveDisplayName(item),
                    false,
                    ProblemKey,
                    ResolveProblemMessage(item, unresolvedToken));
    }

    /// <summary>
    /// Resolves an Item's <b>first</b> line — the umbrella phrase, plus whatever the Item declares beside it
    /// (<see cref="RequestItemDefinition.CardLine1Template"/>). It is the same deliberately-small template
    /// language the identifier uses, with one further token of its own: <c>{umbrella}</c>, the Item's own phrase.
    /// </summary>
    /// <remarks>
    /// There is <b>no</b> problem report here, and that is the point. The first line's job is to say what kind of
    /// request this is, and the phrase alone says it even when the job value beside it is missing — so an
    /// unresolvable token degrades to the phrase rather than turning the card's heading into a configuration
    /// fault. Where a fault <b>is</b> reported is the identifier, which is the line that would otherwise be blank
    /// (FR-005, FR-026).
    /// </remarks>
    public string ResolveLine1(RequestItemDefinition item, RequestItemLine2Context? context = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var template = item.CardLine1Template ?? string.Empty;
        if (template.IndexOf('{') < 0)
        {
            return item.UmbrellaVerb;
        }

        var tokens = BuildTokens(context ?? new RequestItemLine2Context());
        tokens["umbrella"] = item.UmbrellaVerb;

        return TryResolveTemplate(template, tokens, out var text, out _)
            ? text
            : item.UmbrellaVerb;
    }

    /// <summary>
    /// Walks a template once: literal text is copied through and a <c>{token}</c> is replaced by its value.
    /// Returns <c>false</c> when any token is unknown or blank, handing back the offending token so each caller
    /// can decide what to do about it.
    /// </summary>
    /// <remarks>
    /// The language's one conditional — <c>{primary:secondary=conditionValue}</c> — retired with the question it
    /// existed for. It switched <c>pickup-die</c>'s identifier on the captured destination, and a die now always
    /// goes to its home location, so there is no captured value left to condition on (FR-054, D22).
    /// </remarks>
    private static bool TryResolveTemplate(
        string? template,
        Dictionary<string, string?> tokens,
        out string text,
        out string? unresolvedToken)
    {
        unresolvedToken = null;
        text = string.Empty;

        var builder = new System.Text.StringBuilder();
        var source = template ?? string.Empty;
        var index = 0;

        while (index < source.Length)
        {
            var open = source.IndexOf('{', index);
            if (open < 0)
            {
                builder.Append(source, index, source.Length - index);
                break;
            }

            builder.Append(source, index, open - index);

            var close = source.IndexOf('}', open + 1);
            if (close < 0)
            {
                // An unterminated brace is a malformed template, not literal text.
                unresolvedToken = source[open..];
                return false;
            }

            var token = source[(open + 1)..close];

            if (!tokens.TryGetValue(token.Trim(), out var value) || string.IsNullOrWhiteSpace(value))
            {
                unresolvedToken = token;
                return false;
            }

            builder.Append(value);
            index = close + 1;
        }

        var resolved = builder.ToString().Trim();
        if (resolved.Length == 0)
        {
            unresolvedToken = source;
            return false;
        }

        text = resolved;
        return true;
    }

    /// <summary>
    /// The Item's display name, resolved from its resource key (FR-022) with a readable fallback so the card is
    /// never blank.
    /// </summary>
    public static string ResolveDisplayName(RequestItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var localized = item.DisplayNameResourceKey.GetLocalized();
        if (!string.IsNullOrWhiteSpace(localized)
            && !string.Equals(localized, item.DisplayNameResourceKey, StringComparison.Ordinal))
        {
            return localized;
        }

        return !string.IsNullOrWhiteSpace(item.NormalizedName) ? item.NormalizedName : item.Id;
    }

    /// <summary>
    /// The plain-language configuration report shown beside the Item's name when a token could not be resolved.
    /// </summary>
    public static string ResolveProblemMessage(RequestItemDefinition item, string? token)
    {
        const string Fallback =
            "This item's card can't show its identifier because its configuration names a value it doesn't have. Tell a supervisor so it can be checked.";
        var localized = ProblemKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, ProblemKey, StringComparison.Ordinal)
            ? Fallback
            : localized;
    }

    private static Dictionary<string, string?> BuildTokens(RequestItemLine2Context context) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["part_number"] = context.PartNumber,
            ["part_description"] = context.PartDescription,
            ["die_number"] = context.DieNumber,
            ["die_location"] = context.DieLocation,
            // The die's two-part identifier as one value — the number and where the die is — so a row can show both
            // without a separator dangling when the location is unknown (FR-056). It is composed here rather than
            // accepted ready-made, so every caller that can supply a die gets the same formatting.
            ["die"] = RequestDiePart.ComposeLabel(context.DieNumber, context.DieLocation),
            ["dunnage_part"] = context.DunnagePart,
            ["sequence_number"] = context.SequenceNumber,
            ["scrap_type"] = context.ScrapType,
            ["answer"] = context.Answer,
            ["component"] = context.Component,
            ["defect"] = context.Defect,
            // The requesting job's own part number — the part a die is assigned to, which the die Items' first
            // line names. It is deliberately a token of its own: `part_number` means the *subordinate's* number
            // for the coil and flatstock Items, so the two must not be conflated (FR-005).
            ["job_part_number"] = context.JobPartNumber
        };
}
