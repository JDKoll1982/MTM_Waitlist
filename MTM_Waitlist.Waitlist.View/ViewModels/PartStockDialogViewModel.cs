using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>One location row in the part stock window.</summary>
/// <param name="Location">The location name or code.</param>
/// <param name="QuantityText">The quantity held there, formatted for display.</param>
public sealed record PartStockLocationItem(string Location, string QuantityText);

/// <summary>
/// The window opened from the part number on a Deliver Component card: where the part is held, how much is on
/// hand in total, and which system said so.
/// </summary>
public sealed partial class PartStockDialogViewModel : ObservableObject
{
    private const string QuantityFormat = "#,0.##";

    private readonly IPartStockService _partStockService;

    /// <summary>Creates the view model.</summary>
    /// <param name="partStockService">The stock lookup.</param>
    public PartStockDialogViewModel(IPartStockService partStockService)
    {
        ArgumentNullException.ThrowIfNull(partStockService);
        _partStockService = partStockService;
    }

    /// <summary>The window's title.</summary>
    public string TitleText => "PartStock_Dialog.Title".GetLocalized();

    /// <summary>The close button's text.</summary>
    public string CloseText => "PartStock_Dialog.Close".GetLocalized();

    /// <summary>The label above the source badge.</summary>
    public string SourceLabelText => "PartStock_Dialog.SourceLabel".GetLocalized();

    /// <summary>The label above the total.</summary>
    public string TotalLabelText => "PartStock_Dialog.TotalLabel".GetLocalized();

    /// <summary>The location column header.</summary>
    public string LocationHeaderText => "PartStock_Dialog.LocationHeader".GetLocalized();

    /// <summary>The quantity column header.</summary>
    public string QuantityHeaderText => "PartStock_Dialog.QuantityHeader".GetLocalized();

    /// <summary>The text shown while the lookup runs.</summary>
    public string LoadingText => "PartStock_Dialog.Loading".GetLocalized();

    /// <summary>The part the window is about.</summary>
    [ObservableProperty]
    public partial string PartNumber { get; private set; } = string.Empty;

    /// <summary>Whether the lookup is still running.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>The system that answered, in words.</summary>
    [ObservableProperty]
    public partial string SourceText { get; private set; } = string.Empty;

    /// <summary>A line explaining a saved copy, or empty when the answer is live.</summary>
    [ObservableProperty]
    public partial string SourceNoteText { get; private set; } = string.Empty;

    /// <summary>The total quantity on hand, formatted.</summary>
    [ObservableProperty]
    public partial string TotalText { get; private set; } = string.Empty;

    /// <summary>What to say instead of the list when there is nothing to list.</summary>
    [ObservableProperty]
    public partial string EmptyText { get; private set; } = string.Empty;

    /// <summary>The locations that hold stock.</summary>
    public ObservableCollection<PartStockLocationItem> Locations { get; } = new();

    /// <summary>Whether there is at least one location to show.</summary>
    public bool HasLocations => Locations.Count > 0;

    /// <summary>Whether the part-wide total is known and can be shown.</summary>
    public bool HasTotal => !IsLoading && !string.IsNullOrEmpty(TotalText);

    /// <summary>
    /// Whether the window has a stock row to draw — a location, or the total on its own when the location read
    /// returned no row while the part-wide read still answered.
    /// </summary>
    public bool ShowsStockRows => HasLocations || HasTotal;

    /// <summary>Whether to show the empty-state text.</summary>
    public bool HasEmptyText => !IsLoading && !string.IsNullOrEmpty(EmptyText);

    /// <summary>Whether there is a source note to show.</summary>
    public bool HasSourceNote => !string.IsNullOrEmpty(SourceNoteText);

    /// <summary>Whether the answer is ready to show.</summary>
    public bool HasAnswer => !IsLoading && !string.IsNullOrEmpty(SourceText);

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasEmptyText));
        OnPropertyChanged(nameof(HasAnswer));
        OnPropertyChanged(nameof(HasTotal));
        OnPropertyChanged(nameof(ShowsStockRows));
    }

    partial void OnEmptyTextChanged(string value) => OnPropertyChanged(nameof(HasEmptyText));

    partial void OnSourceTextChanged(string value) => OnPropertyChanged(nameof(HasAnswer));

    partial void OnSourceNoteTextChanged(string value) => OnPropertyChanged(nameof(HasSourceNote));

    partial void OnTotalTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasTotal));
        OnPropertyChanged(nameof(ShowsStockRows));
    }

    /// <summary>Looks the part up and fills the window. Never throws.</summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="cancellationToken">A token to cancel the lookup.</param>
    public async Task LoadAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        PartNumber = partNumber?.Trim() ?? string.Empty;
        IsLoading = true;
        Locations.Clear();
        NotifyLocationsChanged();
        EmptyText = string.Empty;

        try
        {
            var stock = await _partStockService.GetStockAsync(PartNumber, cancellationToken);
            Apply(stock);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("PartStock", ex, $"The part stock window could not load part '{PartNumber}'.");
            SourceText = string.Empty;
            EmptyText = "PartStock_Dialog.Unavailable".GetLocalized();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(PartStockSnapshot stock)
    {
        SourceText = SourceTextFor(stock);
        SourceNoteText = stock.IsCachedData
            ? stock.CachedAsOfUtc is { } asOf
                ? string.Format(CultureInfo.CurrentCulture, "PartStock_Dialog.CachedAsOf".GetLocalized(), asOf.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : "PartStock_Dialog.CachedUnknownAge".GetLocalized()
            : string.Empty;

        TotalText = stock.WasRead ? stock.TotalQuantity.ToString(QuantityFormat, CultureInfo.CurrentCulture) : string.Empty;

        foreach (var row in stock.Locations)
        {
            Locations.Add(new PartStockLocationItem(row.Location, row.Quantity.ToString(QuantityFormat, CultureInfo.CurrentCulture)));
        }

        NotifyLocationsChanged();

        if (!stock.WasRead)
        {
            SourceText = string.Empty;
            EmptyText = "PartStock_Dialog.Unavailable".GetLocalized();
        }
        else if (Locations.Count > 0)
        {
            EmptyText = string.Empty;
        }
        else
        {
            // Nothing is hidden from this window's list, so an empty list beside a total can only mean the total
            // came from the part-wide read while the location read returned no row at all. Say that rather than
            // claiming "no stock" beside a figure that says otherwise.
            EmptyText = stock.TotalQuantity > 0m
                ? "PartStock_Dialog.NoLocations".GetLocalized()
                : "PartStock_Dialog.NoStock".GetLocalized();
        }
    }

    private void NotifyLocationsChanged()
    {
        OnPropertyChanged(nameof(HasLocations));
        OnPropertyChanged(nameof(ShowsStockRows));
    }

    private static string SourceTextFor(PartStockSnapshot stock) => stock.Source switch
    {
        PartStockSource.WipApp => "PartStock_Dialog.SourceWipApp".GetLocalized(),
        _ when stock.IsCachedData => "PartStock_Dialog.SourceVisualCached".GetLocalized(),
        _ => "PartStock_Dialog.SourceVisualLive".GetLocalized(),
    };
}
