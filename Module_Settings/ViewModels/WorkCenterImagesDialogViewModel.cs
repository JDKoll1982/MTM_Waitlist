using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Rows for every active work center, grouped by building and keyed by
/// setup_workstations_catalog.id.
/// </summary>
public sealed class WorkCenterImagesDialogViewModel : ImageOverrideDialogViewModel
{
    private readonly IImageOverrideReadService _readService;
    private readonly IImageOverrideWriteService _writeService;
    private readonly ILogger<WorkCenterImagesDialogViewModel> _logger;

    public WorkCenterImagesDialogViewModel(
        IImageLocationService imageLocationService,
        IImageOverrideReadService readService,
        IImageOverrideWriteService writeService,
        IImageStorageService storageService,
        ILogger<WorkCenterImagesDialogViewModel> logger)
        : base(imageLocationService, readService, writeService, storageService, logger)
    {
        _readService = readService;
        _writeService = writeService;
        _logger = logger;
    }

    public override string Scope => "work_center";

    public override string Title => "Work Center Images";

    public override bool SupportsGrouping => true;

    protected override string LoadFailureMessage =>
        "Database unavailable — the work-center catalog could not be loaded. Save is disabled.";

    /// <summary>Overrides whose work center no longer exists, offered for pruning after confirmation.</summary>
    public IReadOnlyList<string> OrphanedItemIds { get; private set; } = Array.Empty<string>();

    public bool HasOrphanedOverrides => OrphanedItemIds.Count > 0;

    protected override async Task<IReadOnlyList<ImageOverrideRow>?> LoadRowsAsync(CancellationToken cancellationToken)
    {
        var workCenters = await ImageLocationService.GetActiveWorkCentersAsync(cancellationToken).ConfigureAwait(true);
        if (workCenters is null)
        {
            return null;
        }

        var rows = workCenters
            .Select(wc => new ImageOverrideRow
            {
                ItemId = wc.WorkCenterId.ToString(),
                DisplayName = wc.DisplayName,
                GroupName = string.IsNullOrWhiteSpace(wc.Building) ? "Unassigned" : wc.Building
            })
            .ToList();

        await DetectOrphansAsync(rows, cancellationToken).ConfigureAwait(true);

        return rows;
    }

    private async Task DetectOrphansAsync(IReadOnlyList<ImageOverrideRow> rows, CancellationToken cancellationToken)
    {
        try
        {
            var liveIds = rows.Select(r => r.ItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var overrides = await _readService.GetOverridesByScopeAsync(Scope, cancellationToken).ConfigureAwait(true);

            OrphanedItemIds = overrides
                .Where(o => !liveIds.Contains(o.ScopeItemId))
                .Select(o => o.ScopeItemId)
                .ToList();

            if (HasOrphanedOverrides)
            {
                StatusMessage = $"{OrphanedItemIds.Count} override(s) point at work centers that no longer exist.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orphan detection failed for scope {Scope}", Scope);
            OrphanedItemIds = Array.Empty<string>();
        }
    }

    /// <summary>Deactivates the orphaned rows. Call only after the user confirms.</summary>
    public async Task<int> PruneOrphanedOverridesAsync(CancellationToken cancellationToken = default)
    {
        var pruned = 0;

        foreach (var itemId in OrphanedItemIds)
        {
            if (await _writeService.DeleteIfExistsAsync(Scope, itemId, null, cancellationToken).ConfigureAwait(true))
            {
                pruned++;
            }
        }

        OrphanedItemIds = Array.Empty<string>();
        StatusMessage = pruned > 0 ? $"Pruned {pruned} orphaned override(s)." : string.Empty;
        return pruned;
    }

    protected override Task<string> ResolveEffectivePathAsync(ImageOverrideRow row, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(row.CustomPath) && File.Exists(row.CustomPath)
            ? Task.FromResult(row.CustomPath)
            : ImageLocationService.ResolveWorkCenterImagePathAsync(row.ItemId, cancellationToken);
}
