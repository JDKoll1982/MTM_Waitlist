using System.Text.Json;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Views;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class WaitlistNewRequestDialogService : IWaitlistNewRequestDialogService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<WaitlistRequestDraft?> ShowJobTypeSelectionAsync(XamlRoot xamlRoot, string building, string selectedWorkCenter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xamlRoot);

        if (string.IsNullOrWhiteSpace(selectedWorkCenter))
        {
            return null;
        }

        var requestTypes = await LoadRequestTypesAsync(cancellationToken).ConfigureAwait(true);
        var dialogItems = requestTypes
            .Where(item => !string.IsNullOrWhiteSpace(item.RequestType))
            .Select(item => new JobTypeDialogItem
            {
                RequestType = item.RequestType.Trim(),
                SubtypeSummary = item.Subtypes.Count == 0
                    ? "No subtype required"
                    : $"{item.Subtypes.Count} subtype option(s)",
            })
            .ToList();

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

        var selectedSubtype = await SelectSubtypeAsync(xamlRoot, selectedRequestType).ConfigureAwait(true);
        if (selectedSubtype is null && selectedRequestType.Subtypes.Count > 0)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled during subtype selection.");
            return null;
        }

        string? inputValue = null;
        if (selectedSubtype?.RequiresTextInput == true)
        {
            inputValue = await PromptForTextInputAsync(xamlRoot, selectedRequestType, selectedSubtype).ConfigureAwait(true);
            if (inputValue is null)
            {
                StartupDebugLog.Info("WaitlistNewRequest", $"Request type '{selectedRequestType.RequestType}' was canceled during text input.");
                return null;
            }
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
        };
    }

    private static async Task<NewRequestSubtypeDefinition?> SelectSubtypeAsync(XamlRoot xamlRoot, NewRequestTypeDefinition requestType)
    {
        if (requestType.Subtypes.Count == 0)
        {
            return null;
        }

        var subtypeNames = requestType.Subtypes
            .Select(item => item.Name)
            .ToList();

        var comboBox = new ComboBox
        {
            Header = "Select a subtype",
            Width = 360,
            Margin = new Thickness(0, 0, 0, 8),
            ItemsSource = subtypeNames,
            SelectedIndex = 0,
        };

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = $"{requestType.RequestType} - Choose subtype",
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = comboBox,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || comboBox.SelectedIndex < 0)
        {
            return null;
        }

        return requestType.Subtypes[comboBox.SelectedIndex];
    }

    private static async Task<string?> PromptForTextInputAsync(XamlRoot xamlRoot, NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition subtype)
    {
        var textBox = new TextBox
        {
            Header = string.IsNullOrWhiteSpace(subtype.PromptText)
                ? $"Enter details for {subtype.Name}"
                : subtype.PromptText,
            MinWidth = 360,
            MaxWidth = 420,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "Enter a short description",
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
                    Text = $"{requestType.RequestType} / {subtype.Name}",
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
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

        dialog.PrimaryButtonClick += (sender, args) =>
        {
            var value = textBox.Text?.Trim() ?? string.Empty;
            if (value.Length < subtype.MinLength || value.Length > subtype.MaxLength)
            {
                validationText.Text = $"Please enter between {subtype.MinLength} and {subtype.MaxLength} characters.";
                args.Cancel = true;
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return textBox.Text?.Trim();
    }

    private static async Task<bool> ShowConfirmationAsync(XamlRoot xamlRoot, string selectedWorkCenter, NewRequestTypeDefinition requestType, NewRequestSubtypeDefinition? subtype, string? inputValue)
    {
        var details = new StackPanel
        {
            Spacing = 8,
        };

        details.Children.Add(new TextBlock
        {
            Text = "Request summary",
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        details.Children.Add(new TextBlock { Text = $"Work Center: {selectedWorkCenter}", TextWrapping = TextWrapping.Wrap });
        details.Children.Add(new TextBlock { Text = $"Request Type: {requestType.RequestType}", TextWrapping = TextWrapping.Wrap });
        if (subtype is not null)
        {
            details.Children.Add(new TextBlock { Text = $"Subtype: {subtype.Name}", TextWrapping = TextWrapping.Wrap });
        }

        if (!string.IsNullOrWhiteSpace(inputValue))
        {
            details.Children.Add(new TextBlock { Text = $"Details: {inputValue}", TextWrapping = TextWrapping.Wrap });
        }

        details.Children.Add(new TextBlock
        {
            Text = "Duplicate requests are allowed with a warning. Continue if this request is intentional.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange),
            TextWrapping = TextWrapping.Wrap,
        });

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = "Confirm request",
            PrimaryButtonText = "Submit",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = details,
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
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
            var parsed = JsonSerializer.Deserialize<List<NewRequestTypeDefinition>>(json, s_jsonOptions);

            if (parsed is not null && parsed.Count > 0)
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

    private static IReadOnlyList<NewRequestTypeDefinition> GetDefaultTypes() =>
    [
        new NewRequestTypeDefinition { RequestType = "Pickup", Control = "MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Other", Control = "MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Coil", Control = "MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Scrap", Control = "MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Flatstock", Control = "MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Table Handling", Control = "MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Die Handling", Control = "MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView" },
        new NewRequestTypeDefinition { RequestType = "Forklift Assist", Control = "MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist.ForkliftAssistRequestTypeImageView" },
    ];
}
