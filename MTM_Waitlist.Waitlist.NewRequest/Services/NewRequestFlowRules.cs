using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure request-flow rules for the New Request wizard (Work Centre → Category → Item → Details → Preview →
/// Summary → Result). These helpers are intentionally static and side-effect free so the wizard view models stay
/// thin and the rules remain directly unit-testable.
/// </summary>
/// <remarks>
/// Re-laid for specs/004-unified-card-item-picker (US1): the type and subtype steps are gone, and the step order
/// is decided by the chosen Item's <b>stored configuration</b> rather than by the Item's identity (FR-013).
/// </remarks>
public static class NewRequestFlowRules
{
    public static EmployeeVerificationResult VerifyEmployeeIdentity(string employeeNumber)
    {
        var normalized = (employeeNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = string.Empty,
                EmployeeName = string.Empty,
                Message = "Employee number is required.",
            };
        }

        if (string.Equals(normalized, "0000", StringComparison.OrdinalIgnoreCase))
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = normalized,
                EmployeeName = "Inactive Employee",
                Message = "This employee is not active and cannot create a request.",
            };
        }

        if (!string.Equals(normalized, "6229", StringComparison.OrdinalIgnoreCase))
        {
            return new EmployeeVerificationResult
            {
                IsValid = false,
                IsActive = false,
                EmployeeNumber = normalized,
                EmployeeName = string.Empty,
                Message = "No active employee was found for that number.",
            };
        }

        return new EmployeeVerificationResult
        {
            IsValid = true,
            IsActive = true,
            EmployeeNumber = normalized,
            EmployeeName = "John Koll",
            Message = "Employee verified.",
        };
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
    /// Resolves which wizard page follows the current accumulated state.
    /// Text-input flows go to Details; no-subtype flows show the intermediate
    /// Preview page; everything else goes straight to the confirmation Summary page.
    /// </summary>
    public static Type GetNextStepType(NewRequestFlowState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Item);

        var configuration = state.ItemConfiguration;
        var requiresAnswer = configuration?.RequiresAnswer == true
            || string.Equals(configuration?.ControlFlow, RequestItemConfiguration.CollectInputThenConfirm, StringComparison.OrdinalIgnoreCase);

        var answerCaptured = !string.IsNullOrWhiteSpace(state.InputValue);
        return requiresAnswer && !answerCaptured
            ? typeof(NewRequestDetailsViewModel)
            : typeof(NewRequestPreviewViewModel);
    }

    private static bool IsNoActiveJobPlaceholder(string? value) =>
        string.Equals(value?.Trim(), "No active job", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value?.Trim(), "No active setup job", StringComparison.OrdinalIgnoreCase);
}

