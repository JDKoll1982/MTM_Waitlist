using System.Text.Json;

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

    public async Task<string?> ShowJobTypeSelectionAsync(XamlRoot xamlRoot, string selectedWorkCenter, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrWhiteSpace(dialog.SelectedRequestType))
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Selected Work Center '{selectedWorkCenter}', selected Job Type '{dialog.SelectedRequestType}'.");

            var progressDialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = "New Request",
                Content = "Job Type selection is now connected. Subtype, input, confirmation, and submit steps are the next increment.",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
            };

            _ = await progressDialog.ShowAsync();
        }

        return dialog.SelectedRequestType;
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
