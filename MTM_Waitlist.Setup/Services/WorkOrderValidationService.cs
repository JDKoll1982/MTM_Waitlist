using System.Text.RegularExpressions;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class WorkOrderValidationService : IWorkOrderValidationService
{
    // The operator's INPUT is deliberately lenient - `76951`, `076951`, `WO-76951` and `WO-076951`
    // all name the same order - and is auto-formatted here to the canonical `WO-######` form before
    // it leaves this service. What the application then sends to Infor Visual (live) and to the
    // `mtm_mock` cache is ALWAYS that canonical `WO` + 6-digit zero-padded string, which is the
    // verbatim BASE_ID of a `W`-family order and the exact key both paths resolve (operator decision
    // 2026-09-12, tasks.md T144). Accepting the loose forms is therefore a formatting convenience,
    // not a widening of the key domain: nothing downstream ever sees the raw input, so the
    // cross-order collisions T144 found (a padded key matching a DIFFERENT order through the retired
    // stripped-form match) cannot come back.
    private static readonly Regex s_workOrderPattern = new("^(?:WO-)?(\\d{5,6})$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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

        normalizedWorkOrder = $"WO-{match.Groups[1].Value.PadLeft(6, '0')}";
        validationMessage = string.Empty;
        return true;
    }
}