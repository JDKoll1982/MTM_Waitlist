using System.Text.RegularExpressions;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class WorkOrderValidationService : IWorkOrderValidationService
{
    // Operator decision, 2026-09-12 (tasks.md T144): the application accepts ONLY the `WO-######`
    // form - the literal `WO-` prefix followed by exactly six digits. The bare numeric forms the rule
    // used to accept (`76951`, `076951`) are no longer expressible, so the key the application asks
    // for is always the verbatim Infor Visual BASE_ID of a `W`-family order. That keeps the live read
    // and the cached copy keyed by exactly the same string: the previous ``^(?:WO-)?(\d{5,6})$`` rule
    // produced a padded `WO-` key for a bare 6-digit base id, which the live predicate could only
    // resolve through its stripped-form match - the source of the cross-order collisions T144 found.
    private static readonly Regex s_workOrderPattern = new("^WO-(\\d{6})$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool TryNormalize(string input, out string normalizedWorkOrder, out string validationMessage)
    {
        var trimmed = (input ?? string.Empty).Trim();
        var match = s_workOrderPattern.Match(trimmed);

        if (!match.Success)
        {
            normalizedWorkOrder = string.Empty;
            validationMessage = "Setup_WorkOrder.Validation.InvalidFormat".GetLocalized();
            return false;
        }

        normalizedWorkOrder = $"WO-{match.Groups[1].Value}";
        validationMessage = string.Empty;
        return true;
    }
}