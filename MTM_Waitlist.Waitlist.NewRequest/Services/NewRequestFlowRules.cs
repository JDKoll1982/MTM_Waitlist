using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure request-flow rules for the New Request wizard (Work Centre → Category → Item → Details → Preview →
/// Summary → Result). These helpers are intentionally static and side-effect free so the wizard view models stay
/// thin and the rules remain directly unit-testable.
/// </summary>
/// <remarks>
/// Re-laid for specs/004-unified-card-item-picker (US1): the type and subtype steps are gone, and the step order
/// is decided by the chosen Item's <b>stored configuration</b> rather than by the Item's identity (FR-013).
/// A step is named by its route key from <see cref="NewRequestStepRoutes"/> rather than by a view-model type, so
/// these rules carry no reference to a view model and the steps do not reach each other through them.
/// </remarks>
public static class NewRequestFlowRules
{
    /// <summary>The label both hopper Items declare their size answer under (FR-077, data-model §9).</summary>
    public const string HopperSizeLabel = "Size";

    /// <summary>The label `assist-coil-turn` declares the operator's coil weight under (FR-086, data-model §9).</summary>
    public const string CoilWeightLabel = "Weight of Coil";

    /// <summary>The label the four returning pickups declare their skid count under (FR-087, data-model §9).</summary>
    public const string SkidCountLabel = "# of Skids";

    /// <summary>The label `deliver-wrong-coil` declares its incorrect coil under (FR-082, data-model §9).</summary>
    public const string IncorrectCoilLabel = "Coil number";

    /// <summary>The label `deliver-wrong-flatstock` declares its incorrect flatstock under (FR-082, data-model §9).</summary>
    public const string IncorrectFlatstockLabel = "Part";

    /// <summary>
    /// Whether the chosen Item's configuration declares an answer under <paramref name="label"/>, which is what a
    /// step checks before it asks its own question: a row that does not declare the field has nothing for the step
    /// to capture and is reported rather than answered into a label nothing reads (FR-013, FR-056).
    /// </summary>
    /// <param name="configuration">The chosen Item's stored behaviour, or null.</param>
    /// <param name="label">The label a step asks its answer under.</param>
    public static bool DeclaresAnswerLabel(RequestItemConfiguration? configuration, string label) =>
        configuration is not null
        && !string.IsNullOrWhiteSpace(label)
        && configuration.DetailFields.Any(authority =>
            string.Equals(authority.Source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase)
            && string.Equals(authority.Label?.Trim(), label.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The label the two wrong-part Items declare their incorrect part under — <c>Coil number</c> on one and
    /// <c>Part</c> on the other — or null when the row declares neither (FR-082, data-model §9).
    /// </summary>
    /// <param name="configuration">The chosen Item's stored behaviour, or null.</param>
    public static string? PartSearchAnswerLabel(RequestItemConfiguration? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        foreach (var label in new[] { IncorrectCoilLabel, IncorrectFlatstockLabel })
        {
            if (DeclaresAnswerLabel(configuration, label))
            {
                return label;
            }
        }

        return null;
    }
    public static (bool IsValid, string Message) ValidateSelectedWorkCenter(string? selectedWorkCenter)
    {
        if (string.IsNullOrWhiteSpace(selectedWorkCenter))
        {
            return (false, "A valid work center is required before continuing.");
        }

        var normalized = selectedWorkCenter.Trim();
        if (IsNoActiveJobPlaceholder(normalized))
        {
            return (false, "No active setup job is available for this work center. Please restart the request after selecting a valid press.");
        }

        return (true, string.Empty);
    }

    public static (bool IsValid, string Message) ValidateCurrentJobState(string? selectedWorkCenter, string? activeSetupJobId)
    {
        if (string.IsNullOrWhiteSpace(selectedWorkCenter))
        {
            return (false, "A valid work center is required before continuing.");
        }

        var normalized = selectedWorkCenter.Trim();
        if (string.Equals(normalized, "No active job", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "No active setup job", StringComparison.OrdinalIgnoreCase)
            || string.Equals(activeSetupJobId?.Trim(), "No active job", StringComparison.OrdinalIgnoreCase)
            || string.Equals(activeSetupJobId?.Trim(), "No active setup job", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "The selected work center no longer has an active setup job. Please restart the request against the current job.");
        }

        if (string.IsNullOrWhiteSpace(activeSetupJobId))
        {
            return (false, "The selected work center no longer has an active setup job. Please restart the request against the current job.");
        }

        return ValidateActiveJobsForWorkCenter(selectedWorkCenter, new[] { activeSetupJobId.Trim() });
    }

    public static (bool IsValid, string Message) ValidateActiveJobsForWorkCenter(string? selectedWorkCenter, IEnumerable<string?> activeSetupJobIds)
    {
        if (string.IsNullOrWhiteSpace(selectedWorkCenter))
        {
            return (false, "A valid work center is required before continuing.");
        }

        var normalized = selectedWorkCenter.Trim();
        if (string.Equals(normalized, "No active job", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "No active setup job", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "The selected work center no longer has an active setup job. Please restart the request against the current job.");
        }

        var candidateJobs = (activeSetupJobIds ?? Enumerable.Empty<string?>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Where(item => !string.Equals(item, "No active job", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item, "No active setup job", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidateJobs.Length == 0)
        {
            return (false, "The selected work center no longer has an active setup job. Please restart the request against the current job.");
        }

        if (candidateJobs.Length > 1)
        {
            return (false, "This work center has multiple active jobs. Please fix the data or configuration before continuing with a waitlist request.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Resolves which wizard page follows the current accumulated state, as the page's route key.
    /// Text-input flows go to Details; once the answer is captured, or where the row asks for nothing at all,
    /// the confirmation Summary page follows and it is the last step before the request is raised.
    /// </summary>
    /// <remarks>
    /// There is no separate review-then-confirm pair: the confirmation step already shows every field the
    /// wizard collected, the coil the job carries, and the count of requests it will raise, so an intermediate
    /// page between the last answer and the submission only repeated it and cost the operator a click.
    /// </remarks>
    public static string GetNextStepRoute(NewRequestFlowState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Item);

        var configuration = state.ItemConfiguration;

        // A declared answer a step of this flow asks is asked by that step, whether or not the row also declares a
        // single answer: the size, the weight of coil, the skid count and the incorrect part each have a step of
        // their own, chosen by the label the ROW declares rather than by the Item's identity (FR-013, FR-082).
        if (PendingAnswerRoute(state) is { } answerStep)
        {
            return answerStep;
        }

        var requiresAnswer = configuration?.RequiresAnswer == true
            || string.Equals(configuration?.ControlFlow, RequestItemConfiguration.CollectInputThenConfirm, StringComparison.OrdinalIgnoreCase);

        var answerCaptured = !string.IsNullOrWhiteSpace(state.InputValue);
        if (!requiresAnswer || answerCaptured)
        {
            return NewRequestStepRoutes.Summary;
        }

        // An answer the job supplies is asked for by the step that can show it: the dunnage list is asked for on
        // the dunnage step, as picture cards of the parts the job carries, with the substitute picker beside them
        // (FR-048, FR-049); the die list on the die step, as cards of the dies the job carries (FR-054); and the
        // component list on the component step, as clickable cards of the components the job carries, so the
        // operator picks a part they can see rather than a number out of a drop-down list.
        // Which list it is comes from the row's declaration, never from the Item's identity (FR-013).
        return RequestItemAnswerOptionsResolver.DeclaredJobListName(configuration) switch
        {
            var list when string.Equals(list, RequestItemFieldDefinition.Lists.Dunnage, StringComparison.OrdinalIgnoreCase)
                => NewRequestStepRoutes.Dunnage,
            var list when string.Equals(list, RequestItemFieldDefinition.Lists.Die, StringComparison.OrdinalIgnoreCase)
                => NewRequestStepRoutes.Die,
            var list when string.Equals(list, RequestItemFieldDefinition.Lists.Component, StringComparison.OrdinalIgnoreCase)
                => NewRequestStepRoutes.Component,
            _ => NewRequestStepRoutes.Details,
        };
    }

    /// <summary>
    /// The step that asks the first answer the row declares that has a step of its own and has not been captured
    /// yet, or null when every such answer is captured — or when the row declares none.
    /// </summary>
    /// <remarks>
    /// Read in declared order, so a row asking two such answers is asked them in the order it declares them. The
    /// die-shop checkbox is deliberately absent from this map: it is asked on the die step, beside the die it
    /// belongs to, rather than on a step of its own (FR-081).
    /// </remarks>
    private static string? PendingAnswerRoute(NewRequestFlowState state)
    {
        var configuration = state.ItemConfiguration;
        if (configuration is null)
        {
            return null;
        }

        foreach (var field in configuration.DetailFields.OrderBy(field => field.Order))
        {
            if (!string.Equals(field.Source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(field.Label)
                || state.HasAnswer(field.Label))
            {
                continue;
            }

            if (RouteForAnswerLabel(field.Label) is { } step)
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// The step a declared answer's label names, or null when no step of this flow asks that answer.
    /// </summary>
    /// <param name="label">The label the row declares the answer under.</param>
    private static string? RouteForAnswerLabel(string label) => label.Trim().ToLowerInvariant() switch
    {
        "size" => NewRequestStepRoutes.Hopper,
        "weight of coil" => NewRequestStepRoutes.CoilTurn,
        "# of skids" => NewRequestStepRoutes.SkidCount,
        "coil number" or "part" => NewRequestStepRoutes.PartSearch,
        _ => null,
    };

    private static bool IsNoActiveJobPlaceholder(string? value) =>
        string.Equals(value?.Trim(), "No active job", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value?.Trim(), "No active setup job", StringComparison.OrdinalIgnoreCase);
}

