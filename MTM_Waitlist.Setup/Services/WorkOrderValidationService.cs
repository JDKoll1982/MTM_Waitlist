using System.Text.RegularExpressions;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class WorkOrderValidationService : IWorkOrderValidationService
{
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