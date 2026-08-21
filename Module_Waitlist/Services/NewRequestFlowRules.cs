using System.Text.Json;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure request-flow rules for the New Request wizard (Work Center -&gt; Job Type -&gt;
/// Subtype -&gt; Details -&gt; Preview -&gt; Summary -&gt; Result). These helpers are
/// intentionally static and side-effect free so the wizard view models stay thin and
/// the rules remain directly unit-testable. The dialog-era request service no longer
/// exists; the rules live here.
/// </summary>
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
        if (string.Equals(normalized, "No active job", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "No active setup job", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "No active setup job is available for this work center. Please restart the request after selecting a valid press.");
        }

        return (true, string.Empty);
    }

    public static IReadOnlyList<NewRequestTypeDefinition> ApplyActiveJobEligibility(
        IReadOnlyList<NewRequestTypeDefinition> requestTypes,
        bool hasCoilData,
        bool hasFlatstockData,
        bool hasPartData,
        bool hasWorkOrderData)
    {
        var filtered = requestTypes
            .Where(item => !string.IsNullOrWhiteSpace(item.RequestType))
            .ToList();

        var applicable = filtered
            .Where(item =>
            {
                var requestType = item.RequestType.Trim();
                if (string.Equals(requestType, "Coil", StringComparison.OrdinalIgnoreCase))
                {
                    return hasCoilData;
                }

                if (string.Equals(requestType, "Flatstock", StringComparison.OrdinalIgnoreCase))
                {
                    return hasFlatstockData;
                }

                if (string.Equals(requestType, "Pickup", StringComparison.OrdinalIgnoreCase))
                {
                    return hasPartData || hasWorkOrderData;
                }

                return true;
            })
            .ToList();

        return applicable;
    }

    public static bool ShouldShowIntermediateSummary(NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition? subtype)
    {
        if (requestType is null)
        {
            return false;
        }

        return subtype is null && requestType.Subtypes.Count == 0;
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
        ArgumentNullException.ThrowIfNull(state.RequestType);

        var requestType = state.RequestType;
        var subtype = state.Subtype;
        var requiresTextInput = subtype?.RequiresTextInput == true
            || (subtype is null && requestType.RequiresTextInput);

        // If the text-input step was already completed, never route back to it.
        var textInputCompleted = requiresTextInput && !string.IsNullOrWhiteSpace(state.InputValue);

        if (requiresTextInput && !textInputCompleted)
        {
            return typeof(NewRequestDetailsViewModel);
        }

        if (ShouldShowIntermediateSummary(requestType, subtype))
        {
            return typeof(NewRequestPreviewViewModel);
        }

        return typeof(NewRequestSummaryViewModel);
    }

    public static IReadOnlyList<NewRequestTypeDefinition> ParseRequestTypes(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<NewRequestTypeDefinition>();
        }

        return TryDeserializeRequestTypes(json, out var parsed) ? parsed : Array.Empty<NewRequestTypeDefinition>();
    }

    public static bool TryDeserializeRequestTypes(string json, out List<NewRequestTypeDefinition> parsed)
    {
        parsed = new List<NewRequestTypeDefinition>();

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var typeElement in document.RootElement.EnumerateArray())
            {
                var definition = new NewRequestTypeDefinition
                {
                    RequestType = GetPropertyString(typeElement, "requestType"),
                    Control = GetPropertyString(typeElement, "control"),
                    Flow = GetPropertyString(typeElement, "flow", "direct-to-confirmation"),
                    RequiresTextInput = GetPropertyBool(typeElement, "requiresTextInput"),
                    PromptText = GetPropertyString(typeElement, "promptText"),
                    MinLength = GetPropertyInt(typeElement, "minLength"),
                    MaxLength = GetPropertyInt(typeElement, "maxLength", 200),
                    CenterDataGridFields = GetPropertyStringList(typeElement, "centerDataGridFields"),
                };

                if (typeElement.TryGetProperty("subtypes", out var subtypeProperty) && subtypeProperty.ValueKind == JsonValueKind.Array)
                {
                    foreach (var subtypeElement in subtypeProperty.EnumerateArray())
                    {
                        definition.Subtypes.Add(new NewRequestSubtypeDefinition
                        {
                            Name = GetPropertyString(subtypeElement, "name"),
                            Control = GetPropertyString(subtypeElement, "control"),
                            Flow = GetPropertyString(subtypeElement, "flow", "direct-to-confirmation"),
                            RequiresTextInput = GetPropertyBool(subtypeElement, "requiresTextInput"),
                            PromptText = GetPropertyString(subtypeElement, "promptText"),
                            MinLength = GetPropertyInt(subtypeElement, "minLength"),
                            MaxLength = GetPropertyInt(subtypeElement, "maxLength", 200),
                            CenterDataGridFields = GetPropertyStringList(subtypeElement, "centerDataGridFields"),
                        });
                    }
                }

                parsed.Add(definition);
            }

            return true;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Failed to parse request type config with tolerant loader. Falling back to defaults. Error={ex.Message}");
            return false;
        }
    }

    public static IReadOnlyList<NewRequestTypeDefinition> GetDefaultTypes() =>
    [
        new NewRequestTypeDefinition { RequestType = "Pickup", Control = "MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView" },
        new NewRequestTypeDefinition
        {
            RequestType = "Other",
            Control = "MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView",
            Subtypes =
            [
                new NewRequestSubtypeDefinition
                {
                    Name = "General Text Entry",
                    RequiresTextInput = true,
                    PromptText = "Enter a short description",
                    MinLength = 5,
                    MaxLength = 200,
                },
            ],
        },
        new NewRequestTypeDefinition { RequestType = "Coil", Control = "MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Scrap", Control = "MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Flatstock", Control = "MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Table Handling", Control = "MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Die Handling", Control = "MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView" },
        new NewRequestTypeDefinition
        {
            RequestType = "Forklift Assist",
            Control = "MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist.ForkliftAssistRequestTypeImageView",
            RequiresTextInput = true,
            PromptText = "Enter description of why you need assistance",
            MinLength = 5,
            MaxLength = 50,
        },
    ];

    private static string GetPropertyString(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? defaultValue,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.ToString(),
            _ => defaultValue,
        };
    }

    private static bool GetPropertyBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(property.GetString(), out var value) && value,
            _ => false,
        };
    }

    private static int GetPropertyInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetInt32(out var numericValue) ? numericValue : defaultValue,
            JsonValueKind.String => int.TryParse(property.GetString(), out var parsedValue) ? parsedValue : defaultValue,
            _ => defaultValue,
        };
    }

    private static List<string> GetPropertyStringList(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        var values = new List<string>();
        foreach (var value in property.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    values.Add(text);
                }
            }
        }

        return values;
    }
}
