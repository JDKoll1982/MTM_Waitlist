using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Shared behaviour for the three image-override dialogs: load, search, filter,
/// per-row reset, reset-all, and a single batched commit on Save.
/// Edits live only in the rows until <see cref="SaveAsync"/> runs, so Cancel is a no-op.
/// </summary>
public abstract partial class ImageOverrideDialogViewModel : ObservableObject
{
    private readonly IImageOverrideReadService _readService;
    private readonly IImageOverrideWriteService _writeService;
    private readonly IImageStorageService _storageService;
    private readonly ILogger _logger;

    private readonly List<ImageOverrideRow> _allRows = new();

    protected ImageOverrideDialogViewModel(
        IImageLocationService imageLocationService,
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        ILogger logger)
    {
        ImageLocationService = imageLocationService ?? throw new ArgumentNullException(nameof(imageLocationService));
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _writeService = writeService ?? throw new ArgumentNullException(nameof(writeService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected IImageLocationService ImageLocationService { get; }

    /// <summary>Value written to config_images_locations.scope.</summary>
    public abstract string Scope { get; }

    public abstract string Title { get; }

    /// <summary>Groups are only rendered when the scope actually groups its rows.</summary>
    public virtual bool SupportsGrouping => false;

    public ObservableCollection<ImageOverrideRowGroup> Groups { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowOnlyCustomImages { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>Save is blocked whenever the row set could not be loaded.</summary>
    public bool CanSave => !HasError && !IsLoading;

    public bool HasRows => Groups.Any(g => g.Rows.Any(r => r.IsEditable));

    public bool IsEmpty => !IsLoading && !HasError && !HasRows;

    /// <summary>Produces the full row set. Return null to signal an unavailable data source.</summary>
    protected abstract Task<IReadOnlyList<ImageOverrideRow>?> LoadRowsAsync(CancellationToken cancellationToken);

    /// <summary>Message shown when <see cref="LoadRowsAsync"/> returns null.</summary>
    protected virtual string LoadFailureMessage => "The data source is unavailable. Save is disabled.";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            if (!ImageLocationService.IsInitialized)
            {
                await ImageLocationService.InitializeAsync(cancellationToken).ConfigureAwait(true);
            }

            var rows = await LoadRowsAsync(cancellationToken).ConfigureAwait(true);
            if (rows is null)
            {
                ErrorMessage = LoadFailureMessage;
                _allRows.Clear();
                ApplyFilter();
                return;
            }

            _allRows.Clear();
            _allRows.AddRange(rows);

            await HydrateRowsAsync(cancellationToken).ConfigureAwait(true);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load image override rows for scope {Scope}", Scope);
            ErrorMessage = "The image override list could not be loaded. Save is disabled.";
            _allRows.Clear();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private async Task HydrateRowsAsync(CancellationToken cancellationToken)
    {
        var overrides = await _readService.GetOverridesByScopeAsync(Scope, cancellationToken).ConfigureAwait(true);
        var byItemId = overrides.ToDictionary(o => o.ScopeItemId, o => o.ImagePath, StringComparer.OrdinalIgnoreCase);

        foreach (var row in _allRows.Where(r => r.IsEditable))
        {
            row.SetPersistedPath(byItemId.TryGetValue(row.ItemId, out var path) ? path : string.Empty);
            await RefreshRowPresentationAsync(row, cancellationToken).ConfigureAwait(true);
        }
    }

    /// <summary>Recomputes the preview path, inherited flag, and missing-file warning for one row.</summary>
    protected async Task RefreshRowPresentationAsync(ImageOverrideRow row, CancellationToken cancellationToken)
    {
        row.EffectiveImagePath = await ResolveEffectivePathAsync(row, cancellationToken).ConfigureAwait(true);
        row.IsInherited = SupportsInheritance && string.IsNullOrWhiteSpace(row.CustomPath);

        var candidate = row.CustomPath;
        row.WarningMessage = !string.IsNullOrWhiteSpace(candidate) && !File.Exists(candidate)
            ? "This file is missing. The default image is shown until it is replaced."
            : string.Empty;
    }

    /// <summary>Only subtypes inherit from a parent scope.</summary>
    protected virtual bool SupportsInheritance => false;

    protected abstract Task<string> ResolveEffectivePathAsync(ImageOverrideRow row, CancellationToken cancellationToken);

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnShowOnlyCustomImagesChanged(bool value) => ApplyFilter();

    partial void OnIsLoadingChanged(bool value) => NotifyStateChanged();

    partial void OnErrorMessageChanged(string value) => NotifyStateChanged();

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    private void ApplyFilter()
    {
        var search = SearchText?.Trim() ?? string.Empty;

        var matching = _allRows.Where(row =>
            row.IsPlaceholder
            || ((string.IsNullOrEmpty(search)
                    || row.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || row.GroupName.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (!ShowOnlyCustomImages || row.HasCustomImage)));

        var grouped = matching
            .GroupBy(r => r.GroupName)
            .Select(g => new ImageOverrideRowGroup
            {
                Key = string.IsNullOrWhiteSpace(g.Key) ? Title : g.Key,
                Rows = g.Where(r => !r.IsPlaceholder || g.Count(x => !x.IsPlaceholder) == 0).ToList()
            })
            // A group whose only real rows were filtered out is noise, but a genuinely empty
            // parent must still show its placeholder.
            .Where(g => g.Rows.Count > 0)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Groups.Clear();
        foreach (var group in grouped)
        {
            Groups.Add(group);
        }

        NotifyStateChanged();
    }

    [RelayCommand]
    private async Task ResetRowAsync(ImageOverrideRow? row)
    {
        if (row is null || !row.IsEditable)
        {
            return;
        }

        row.Reset();
        await RefreshRowPresentationAsync(row, CancellationToken.None).ConfigureAwait(true);
        ApplyFilter();
    }

    /// <summary>Clears every pending override. The caller is responsible for confirming first.</summary>
    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var row in _allRows.Where(r => r.IsEditable))
        {
            row.Reset();
            await RefreshRowPresentationAsync(row, cancellationToken).ConfigureAwait(true);
        }

        ApplyFilter();
        StatusMessage = "All overrides cleared. Choose Save to apply.";
    }

    /// <summary>Drops every pending edit; nothing has been written at this point.</summary>
    public void CancelEdits()
    {
        foreach (var row in _allRows)
        {
            row.RevertEdits();
        }

        StatusMessage = string.Empty;
    }

    public void SetRowPath(ImageOverrideRow row, string path)
    {
        row.CustomPath = path;
        _ = RefreshRowPresentationAsync(row, CancellationToken.None);
        ApplyFilter();
    }

    /// <summary>
    /// Commits every dirty row in one pass. Returns false and leaves the dialog open
    /// if the share is unreachable or any row fails.
    /// </summary>
    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!CanSave)
        {
            return false;
        }

        var dirtyRows = _allRows.Where(r => r.IsEditable && r.IsDirty).ToList();
        if (dirtyRows.Count == 0)
        {
            return true;
        }

        var needsShare = dirtyRows.Any(r => !string.IsNullOrWhiteSpace(r.CustomPath));
        if (needsShare && !await _storageService.IsShareAccessibleAsync(cancellationToken).ConfigureAwait(true))
        {
            ErrorMessage = $"The image share '{_storageService.GetConfiguredSharePath()}' is unavailable. No changes were saved.";
            return false;
        }

        var failures = new List<string>();

        foreach (var row in dirtyRows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.CustomPath))
                {
                    var deleted = await _writeService.DeleteIfExistsAsync(Scope, row.ItemId, null, cancellationToken).ConfigureAwait(true);
                    if (deleted)
                    {
                        row.SetPersistedPath(string.Empty);
                    }

                    continue;
                }

                var stored = await _storageService
                    .ValidateAndStoreImageAsync(row.CustomPath, Scope, row.ItemId, cancellationToken)
                    .ConfigureAwait(true);

                if (!stored.Success)
                {
                    failures.Add($"{row.DisplayName}: {stored.ErrorMessage}");
                    continue;
                }

                var storedPath = stored.StoredFilePath ?? row.CustomPath;

                var existing = await _readService.GetOverrideAsync(Scope, row.ItemId, cancellationToken).ConfigureAwait(true);
                var result = existing is null
                    ? await _writeService.CreateOverrideAsync(Scope, row.ItemId, storedPath, null, cancellationToken).ConfigureAwait(true)
                    : await _writeService.UpdateOverrideAsync(Scope, row.ItemId, storedPath, null, cancellationToken).ConfigureAwait(true);

                if (!result.Success)
                {
                    failures.Add($"{row.DisplayName}: {result.ErrorMessage}");
                    continue;
                }

                row.SetPersistedPath(storedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save override for {Scope}:{ItemId}", Scope, row.ItemId);
                failures.Add($"{row.DisplayName}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            ErrorMessage = "Some images could not be saved:" + Environment.NewLine + string.Join(Environment.NewLine, failures);
            return false;
        }

        foreach (var row in dirtyRows)
        {
            await RefreshRowPresentationAsync(row, cancellationToken).ConfigureAwait(true);
        }

        ApplyFilter();
        return true;
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
