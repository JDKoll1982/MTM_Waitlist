using System.Text.Json;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Views;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class WaitlistNewRequestDialogService : IWaitlistNewRequestDialogService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IImageLocationService _imageLocationService;

    public WaitlistNewRequestDialogService(IImageLocationService imageLocationService)
    {
        _imageLocationService = imageLocationService;
    }

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

    private async Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return string.Empty;
        }

        try
        {
            var requestType = RequestTypeInventory.GetByDisplayName(requestTypeName);
            if (requestType is null)
            {
                return string.Empty;
            }

            return await _imageLocationService
                .ResolveRequestTypeImagePathAsync(requestType.StableId.ToString(), cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private async Task<string> ResolveRequestSubtypeImagePathAsync(
        string requestTypeName,
        string subtypeName,
        CancellationToken cancellationToken)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return string.Empty;
        }

        try
        {
            var (_, subtype) = RequestSubtypeInventory.GetByDisplayNames(requestTypeName, subtypeName);
            if (subtype is null)
            {
                return string.Empty;
            }

            return await _imageLocationService
                .ResolveRequestSubtypeImagePathAsync(subtype.StableId.ToString(), cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            return string.Empty;
        }
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

    public async Task<WaitlistRequestDraft?> ShowJobTypeSelectionAsync(XamlRoot xamlRoot, string building, string selectedWorkCenter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xamlRoot);

        var selectedWorkCenterValidation = ValidateSelectedWorkCenter(selectedWorkCenter);
        if (!selectedWorkCenterValidation.IsValid)
        {
            await ShowMessageAsync(xamlRoot, "Work center validation", selectedWorkCenterValidation.Message).ConfigureAwait(true);
            return null;
        }

        var employeeVerification = VerifyEmployeeIdentity("6229");
        if (!employeeVerification.IsValid)
        {
            await ShowMessageAsync(xamlRoot, "Employee verification required", employeeVerification.Message).ConfigureAwait(true);
            return null;
        }

        var requestTypes = ApplyActiveJobEligibility(
            await LoadRequestTypesAsync(cancellationToken).ConfigureAwait(true),
            hasCoilData: true,
            hasFlatstockData: true,
            hasPartData: true,
            hasWorkOrderData: true);

        var dialogItems = new List<JobTypeDialogItem>();
        foreach (var requestType in requestTypes.Where(item => !string.IsNullOrWhiteSpace(item.RequestType)))
        {
            var requestTypeName = requestType.RequestType.Trim();
            dialogItems.Add(new JobTypeDialogItem
            {
                RequestType = requestTypeName,
                SubtypeSummary = requestType.Subtypes.Count == 0
                    ? "No subtype required"
                    : $"{requestType.Subtypes.Count} subtype option(s)",
                ImagePath = await ResolveRequestTypeImagePathAsync(requestTypeName, cancellationToken).ConfigureAwait(true),
            });
        }

        var dialog = new NewRequestJobTypeDialog
        {
            XamlRoot = xamlRoot,
        };

        dialog.SetContent(selectedWorkCenter.Trim(), dialogItems);
        _ = await dialog.ShowAsync();

        if (string.IsNullOrWhiteSpace(dialog.SelectedRequestType))
        {
            return null;
        }

        var selectedRequestType = requestTypes.FirstOrDefault(item => string.Equals(item.RequestType.Trim(), dialog.SelectedRequestType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (selectedRequestType is null)
        {
            await ShowMessageAsync(xamlRoot, "Unable to continue", "The selected request type could not be resolved. Please try again.").ConfigureAwait(true);
            return null;
        }

        StartupDebugLog.Info("WaitlistNewRequest", $"Selected Work Center '{selectedWorkCenter}', selected Job Type '{dialog.SelectedRequestType}'.");

        var selectedSubtype = await SelectSubtypeAsync(xamlRoot, selectedRequestType, cancellationToken).ConfigureAwait(true);
        if (selectedSubtype is null && selectedRequestType.Subtypes.Count > 0)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled during subtype selection.");
            return null;
        }

        string? inputValue = null;
        if (selectedSubtype?.RequiresTextInput == true || (selectedSubtype is null && selectedRequestType.RequiresTextInput))
        {
            inputValue = await PromptForTextInputAsync(xamlRoot, selectedRequestType, selectedSubtype).ConfigureAwait(true);
            if (inputValue is null)
            {
                StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled during text input.");
                return null;
            }
        }

        if (ShouldShowIntermediateSummary(selectedRequestType, selectedSubtype))
        {
            var summaryConfirmed = await ShowRequestSummaryAsync(xamlRoot, selectedWorkCenter, selectedRequestType, selectedSubtype, inputValue).ConfigureAwait(true);
            if (!summaryConfirmed)
            {
                StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled at the intermediate summary step.");
                return null;
            }
        }

        var finalJobState = ValidateCurrentJobState(selectedWorkCenter, selectedWorkCenter);
        if (!finalJobState.IsValid)
        {
            await ShowMessageAsync(xamlRoot, "Current job changed", finalJobState.Message).ConfigureAwait(true);
            StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was blocked because the active job changed for work center '{selectedWorkCenter}'.");
            return null;
        }

        var confirmed = await ShowConfirmationAsync(xamlRoot, selectedWorkCenter, selectedRequestType, selectedSubtype, inputValue).ConfigureAwait(true);
        if (!confirmed)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled at confirmation.");
            return null;
        }

        StartupDebugLog.Info("WaitlistNewRequest", $"Request confirmed. WorkCenter='{selectedWorkCenter}', RequestType='{selectedRequestType.RequestType}', Subtype='{selectedSubtype?.Name ?? string.Empty}'.");

        return new WaitlistRequestDraft
        {
            Building = building.Trim(),
            WorkCenter = selectedWorkCenter.Trim(),
            RequestType = selectedRequestType.RequestType.Trim(),
            Subtype = selectedSubtype?.Name,
            InputValue = inputValue,
            ActiveSetupJobId = selectedWorkCenter.Trim(),
            WorkstationName = selectedWorkCenter.Trim(),
            RequesterEmployeeNumber = employeeVerification.EmployeeNumber,
            RequesterEmployeeName = employeeVerification.EmployeeName,
            RequestedUtc = DateTimeOffset.UtcNow,
        };
    }

    private async Task<NewRequestSubtypeDefinition?> SelectSubtypeAsync(
        XamlRoot xamlRoot,
        NewRequestTypeDefinition requestType,
        CancellationToken cancellationToken)
    {
        if (requestType.Subtypes.Count == 0)
        {
            return null;
        }

        var dialogItems = new List<JobTypeDialogItem>();
        foreach (var subtype in requestType.Subtypes)
        {
            var subtypeName = subtype.Name.Trim();
            dialogItems.Add(new JobTypeDialogItem
            {
                RequestType = subtypeName,
                SubtypeSummary = subtypeName,
                ImagePath = await ResolveRequestSubtypeImagePathAsync(
                    requestType.RequestType,
                    subtypeName,
                    cancellationToken).ConfigureAwait(true),
            });
        }

        var dialog = new NewRequestSubtypeDialog
        {
            XamlRoot = xamlRoot,
        };

        dialog.SetContent(requestType.RequestType.Trim(), dialogItems);
        _ = await dialog.ShowAsync();

        if (string.IsNullOrWhiteSpace(dialog.SelectedSubtypeName))
        {
            return null;
        }

        return requestType.Subtypes.FirstOrDefault(subtype =>
            string.Equals(subtype.Name.Trim(), dialog.SelectedSubtypeName, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string?> PromptForTextInputAsync(XamlRoot xamlRoot, NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition? subtype)
    {
        var targetName = subtype is not null ? subtype.Name : requestType.RequestType;
        var promptText = subtype is not null ? subtype.PromptText : requestType.PromptText;
        var minLength = subtype is not null ? subtype.MinLength : requestType.MinLength;
        var maxLength = subtype is not null ? subtype.MaxLength : requestType.MaxLength;

        var textBox = new TextBox
        {
            Header = string.IsNullOrWhiteSpace(promptText)
                ? $"Enter details for {targetName}"
                : promptText,
            MinWidth = 360,
            MaxWidth = 420,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "Enter a short description",
        };

        var summaryText = new TextBlock
        {
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6),
            Visibility = Visibility.Collapsed,
        };

        var validationText = new TextBlock
        {
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0),
        };

        var panel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = subtype is null ? requestType.RequestType : $"{requestType.RequestType} / {subtype.Name}",
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
                summaryText,
                textBox,
                validationText,
            },
        };

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = "Additional details",
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
        };

        RequestDialogStyling.Apply(dialog);

        dialog.PrimaryButtonClick += (sender, args) =>
        {
            var value = textBox.Text?.Trim() ?? string.Empty;
            if (value.Length < minLength || value.Length > maxLength)
            {
                var summary = $"Please enter between {minLength} and {maxLength} characters.";
                summaryText.Text = summary;
                summaryText.Visibility = Visibility.Visible;
                validationText.Text = summary;
                args.Cancel = true;
            }
            else
            {
                summaryText.Text = string.Empty;
                summaryText.Visibility = Visibility.Collapsed;
                validationText.Text = string.Empty;
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return textBox.Text?.Trim();
    }

    private static async Task<bool> ShowRequestSummaryAsync(XamlRoot xamlRoot, string selectedWorkCenter, NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition? subtype, string? inputValue)
    {
        var summaryRows = new StackPanel { Spacing = 8 };
        summaryRows.Children.Add(CreateDialogHeading("Request preview"));
        summaryRows.Children.Add(CreateDialogFieldRow("Work Center", selectedWorkCenter));
        summaryRows.Children.Add(CreateDialogFieldRow("Request Type", requestType.RequestType));
        if (!string.IsNullOrWhiteSpace(inputValue))
        {
            summaryRows.Children.Add(CreateDialogFieldRow("Detail", inputValue));
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = "Request details",
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = CreateDialogCard(summaryRows),
        };

        RequestDialogStyling.Apply(dialog);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static async Task<bool> ShowConfirmationAsync(XamlRoot xamlRoot, string selectedWorkCenter, NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition? subtype, string? inputValue)
    {
        var content = new StackPanel
        {
            Spacing = 12,
        };

        // Request summary card
        var summaryRows = new StackPanel { Spacing = 8 };
        summaryRows.Children.Add(CreateDialogHeading("Request summary"));
        summaryRows.Children.Add(CreateDialogFieldRow("Work Center", selectedWorkCenter));
        summaryRows.Children.Add(CreateDialogFieldRow("Request Type", requestType.RequestType));
        if (subtype is not null)
        {
            summaryRows.Children.Add(CreateDialogFieldRow("Subtype", subtype.Name));
        }

        if (!string.IsNullOrWhiteSpace(inputValue))
        {
            summaryRows.Children.Add(CreateDialogFieldRow("Details", inputValue));
        }

        content.Children.Add(CreateDialogCard(summaryRows));

        // Queue and wait-time card
        var infoRows = new StackPanel { Spacing = 6 };
        infoRows.Children.Add(CreateDialogHeading("Queue & wait time"));
        infoRows.Children.Add(CreateDialogLine("0 active request(s) for this work center."));
        infoRows.Children.Add(CreateDialogLine("Estimated wait time: approximately 15 minutes."));

        content.Children.Add(CreateDialogCard(infoRows));

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = "Confirm request",
            PrimaryButtonText = "Submit",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
        };

        RequestDialogStyling.Apply(dialog);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static Border CreateDialogCard(UIElement child)
    {
        return new Border
        {
            Background = RequestDialogStyling.GetBrush("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = RequestDialogStyling.GetBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Child = child,
        };
    }

    private static TextBlock CreateDialogHeading(string text)
    {
        return new TextBlock
        {
            Text = text,
            Style = RequestDialogStyling.GetStyle("RequestDialogTitleStyle"),
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private static Grid CreateDialogFieldRow(string label, string value)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = RequestDialogStyling.GetStyle("RequestDialogFieldLabelStyle"),
            TextWrapping = TextWrapping.Wrap,
        };
        grid.Children.Add(labelBlock);
        Grid.SetColumn(labelBlock, 0);

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = RequestDialogStyling.GetStyle("RequestDialogFieldValueStyle"),
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
        };
        grid.Children.Add(valueBlock);
        Grid.SetColumn(valueBlock, 1);

        return grid;
    }

    private static TextBlock CreateDialogLine(string text)
    {
        return new TextBlock
        {
            Text = text,
            Style = RequestDialogStyling.GetStyle("RequestDialogFieldValueStyle"),
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private static async Task ShowMessageAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = title,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
            },
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
        };

        RequestDialogStyling.Apply(dialog);

        _ = await dialog.ShowAsync();
    }

    private static async Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "waitlist-request-types.json");

        if (!File.Exists(configPath))
        {
            return GetDefaultTypes();
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
            var parsed = ParseRequestTypes(json);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Failed to load request type config. Falling back to defaults. Error={ex.Message}");
        }

        return GetDefaultTypes();
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

    private static IReadOnlyList<NewRequestTypeDefinition> GetDefaultTypes() =>
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
}
