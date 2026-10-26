using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The lugger arrangement's Settings screen: every lugger with its status and the scrap types it takes, the dump
/// point each scrap type resolves to, and the editor behind an explicit action (FR-001, FR-003, FR-030, FR-031).
/// </summary>
/// <remarks>
/// <para>
/// <b>Two reads, one summary.</b> The lugger list comes from <c>sp_waitlist_lugger_locations_list</c> and the
/// pairings from <c>sp_waitlist_lugger_arrangement_list</c>; the summary is built from those two and nothing is
/// kept between loads, so a change made elsewhere shows on the next load rather than being served from a copy.
/// </para>
/// <para>
/// <b>Every write carries the actor.</b> Each command passes the signed-in person's employee number and name to
/// the procedure, because the store records who made the change in the same transaction that makes it (FR-032).
/// A refusal the store raises is shown in the store's own words; any other failure is reported as the arrangement
/// being unavailable, with a manual retry and no sample lugger (constitution II).
/// </para>
/// <para>
/// <b>The gate is the permission, and it is read here too.</b> The entry on the Settings screen is drawn only for
/// a holder, but reaching this screen another way is refused on the same answer rather than trusting the entry's
/// absence (FR-028, FR-029).
/// </para>
/// </remarks>
public partial class LuggerArrangementViewModel : ObservableRecipient, INavigationAware
{
    private readonly ILuggerArrangementService _arrangementService;
    private readonly IPermissionService _permissionService;
    private readonly IPersonIdentity _personIdentity;
    private readonly ILuggerImageResolver? _luggerImages;
    private readonly ILuggerBackupPrompt? _backupPrompt;

    private LuggerArrangementSnapshot _snapshot = LuggerArrangementSnapshot.Empty;

    public LuggerArrangementViewModel(
        ILuggerArrangementService arrangementService,
        IPermissionService permissionService,
        IPersonIdentity personIdentity,
        ILuggerImageResolver? luggerImages = null,
        ILuggerBackupPrompt? backupPrompt = null)
    {
        _arrangementService = arrangementService ?? throw new ArgumentNullException(nameof(arrangementService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _personIdentity = personIdentity ?? throw new ArgumentNullException(nameof(personIdentity));
        _luggerImages = luggerImages;
        _backupPrompt = backupPrompt;
    }

    /// <summary>The work the page's arrival started, so a page or a test can wait for the first read.</summary>
    public Task Initialization { get; private set; } = Task.CompletedTask;

    /// <summary>Every lugger, in the arrangement's own order, with its status and the scrap types it takes.</summary>
    public ObservableCollection<LuggerArrangementRow> Luggers { get; } = new();

    /// <summary>Every scrap type the arrangement knows, with the lugger that holds its dump point (FR-031).</summary>
    public ObservableCollection<ScrapTypeDumpPointRow> ScrapTypes { get; } = new();

    /// <summary>
    /// What the scrap-type field offers: the shipped catalogue, then any type already assigned that the catalogue
    /// does not name. The field itself accepts a spelling none of these carry (FR-011).
    /// </summary>
    public ObservableCollection<string> ScrapTypeOptions { get; } = new();

    /// <summary>The four statuses a lugger may carry, in the words the procedures accept.</summary>
    public IReadOnlyList<string> StatusOptions { get; } =
    [
        LuggerStatus.Available,
        LuggerStatus.Backup,
        LuggerStatus.Full,
        LuggerStatus.DoNotUse,
    ];

    /// <summary>Whether the reader holds <c>permission.settings.lugger_arrangement</c> (FR-028).</summary>
    [ObservableProperty]
    public partial bool IsEntitled
    {
        get; set;
    }

    /// <summary>Whether the reader may not open the arrangement, which the page states rather than hiding.</summary>
    public bool NotEntitled => !IsEntitled;

    /// <summary>True while a read or a write is under way, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>Whether the store could not be reached, which the page states with a manual retry.</summary>
    [ObservableProperty]
    public partial bool IsStoreUnavailable
    {
        get; set;
    }

    /// <summary>What the screen says about the last thing that happened, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    /// <summary>The lugger the editor's rename, remove, status and pairing actions apply to.</summary>
    [ObservableProperty]
    public partial LuggerArrangementRow? SelectedLugger
    {
        get; set;
    }

    /// <summary>The name the add and rename actions write.</summary>
    [ObservableProperty]
    public partial string LuggerNameInput
    {
        get; set;
    } = string.Empty;

    /// <summary>The status the status action writes, one of the four pinned words.</summary>
    [ObservableProperty]
    public partial string StatusInput
    {
        get; set;
    } = LuggerStatus.Available;

    /// <summary>The reason the status action writes; required for <c>Do Not Use</c> alone (FR-019).</summary>
    [ObservableProperty]
    public partial string ReasonInput
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// The scrap type the pairing actions name, either chosen from <see cref="ScrapTypeOptions"/> or typed, so a
    /// type the catalogue does not name can be recorded (FR-011).
    /// </summary>
    [ObservableProperty]
    public partial string ScrapTypeInput
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the reason field is required for the chosen status, which is <c>Do Not Use</c> alone.</summary>
    public bool IsReasonRequired => LuggerStatus.RequiresReason(StatusInput);

    /// <summary>
    /// Whether the chosen lugger can be made the dump point for the chosen scrap type: the type must be assigned
    /// to it, another lugger must be assigned as its backup, and this one must not already hold the role.
    /// </summary>
    /// <remarks>
    /// Promoting a lugger that is already the dump point changes nothing, and promoting one whose type has no
    /// backup would leave the button offering a move that cannot happen. The action is therefore drawn only when
    /// it can actually do something (FR-006, FR-018).
    /// </remarks>
    public bool CanPromote
    {
        get
        {
            if (SelectedLugger is null)
            {
                return false;
            }

            var key = Normalise(ScrapTypeInput);
            if (key.Length == 0)
            {
                return false;
            }

            var assigned = _snapshot.Rows
                .Where(row => string.Equals(row.ScrapTypeKey, key, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(row.LuggerPublicId))
                .ToArray();

            if (assigned.Length < 2)
            {
                return false;
            }

            var mine = assigned.FirstOrDefault(row =>
                string.Equals(row.LuggerPublicId, SelectedLugger.PublicId, StringComparison.Ordinal));

            return mine is not null && mine.IsDumpPoint != true;
        }
    }

    public string TitleText => "Settings_Lugger_Arrangement.Title".GetLocalized();

    public string EditActionText => "Settings_Lugger_Arrangement.EditAction".GetLocalized();

    public string CloseEditorActionText => "Settings_Lugger_Arrangement.CloseEditorAction".GetLocalized();

    /// <summary>The name dialog's dismissing action.</summary>
    public string CancelActionText => "Settings_Lugger_Arrangement.CancelAction".GetLocalized();

    public string NameLabelText => "Settings_Lugger_Arrangement.NameLabel".GetLocalized();

    public string StatusLabelText => "Settings_Lugger_Arrangement.StatusLabel".GetLocalized();

    public string ScrapTypeLabelText => "Settings_Lugger_Arrangement.ScrapTypeLabel".GetLocalized();

    public string NoScrapTypesText => "Settings_Lugger_Arrangement.NoScrapTypes".GetLocalized();

    public string AddActionText => "Settings_Lugger_Arrangement.AddAction".GetLocalized();

    public string RenameActionText => "Settings_Lugger_Arrangement.RenameAction".GetLocalized();

    public string RemoveActionText => "Settings_Lugger_Arrangement.RemoveAction".GetLocalized();

    public string StatusActionText => "Settings_Lugger_Arrangement.StatusAction".GetLocalized();

    public string AssignActionText => "Settings_Lugger_Arrangement.AssignAction".GetLocalized();

    public string UnassignActionText => "Settings_Lugger_Arrangement.UnassignAction".GetLocalized();

    public string PromoteActionText => "Settings_Lugger_Arrangement.PromoteAction".GetLocalized();

    public string ReasonLabelText => "Settings_Lugger_Arrangement.ReasonLabel".GetLocalized();

    public string ReasonRequiredText => "Settings_Lugger_Arrangement.ReasonRequired".GetLocalized();

    public string UnavailableText => "Settings_Lugger_Arrangement.Unavailable".GetLocalized();

    /// <summary>What a write that did not land says, which is not the same news as a store that cannot be read.</summary>
    public string WriteFailedText => "Settings_Lugger_Arrangement.WriteFailed".GetLocalized();

    /// <summary>What the page says when the store is behind this build, so the fix is obvious rather than retried.</summary>
    public string StoreOutOfDateText => "Settings_Lugger_Arrangement.StoreOutOfDate".GetLocalized();

    public string RetryActionText => "Settings_Lugger_Arrangement.RetryAction".GetLocalized();

    public string RefusedText => "Settings_Lugger_Arrangement.Refused".GetLocalized();

    /// <summary>Whether an action may write: the reader holds the key and nothing else is in flight.</summary>
    private bool CanWrite() => IsEntitled && !IsBusy;

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync();

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>The page's arrival: the reader's entitlement, then the arrangement.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;
        MessageText = string.Empty;

        try
        {
            IsEntitled = await _permissionService
                .HasPermissionAsync(PermissionKeys.SettingsLuggerArrangement, cancellationToken)
                .ConfigureAwait(true);

            if (!IsEntitled)
            {
                MessageText = RefusedText;
                ClearArrangement();
                return;
            }

            await LoadArrangementAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ShowUnavailable(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The reader's own retry, which reads the arrangement again on the same entitlement.</summary>
    [RelayCommand]
    public Task LoadAsync(CancellationToken cancellationToken = default) => InitializeAsync(cancellationToken);

    /// <summary>
    /// Points the editor's actions at one lugger: the card's own Edit or Rename action names it, so the page never
    /// asks the reader to pick the lugger again (FR-030).
    /// </summary>
    /// <param name="lugger">The lugger whose card was acted on.</param>
    public void SelectLugger(LuggerArrangementRow lugger)
    {
        ArgumentNullException.ThrowIfNull(lugger);

        SelectedLugger = lugger;
        LuggerNameInput = lugger.Name;
        StatusInput = lugger.Status;
        ReasonInput = lugger.StatusReason ?? string.Empty;
        MessageText = string.Empty;
    }

    /// <summary>Points the rename action at one lugger, with its current name already in the field.</summary>
    /// <param name="lugger">The lugger whose card was acted on.</param>
    public void BeginRename(LuggerArrangementRow lugger) => SelectLugger(lugger);

    /// <summary>Clears the name field for a lugger that does not exist yet.</summary>
    public void BeginAdd()
    {
        LuggerNameInput = string.Empty;
        MessageText = string.Empty;
    }

    /// <summary>Adds a lugger after the ones already there (FR-003).</summary>
    [RelayCommand]
    public async Task AddLuggerAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite())
        {
            return;
        }

        var name = LuggerNameInput.Trim();
        if (name.Length == 0)
        {
            MessageText = RefusedText;
            return;
        }

        var position = Luggers.Count == 0 ? 1 : Luggers.Max(row => row.DisplayPosition) + 1;

        await ApplyAsync(
            () => _arrangementService.AddLuggerAsync(
                name,
                position,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Renames the chosen lugger (FR-003).</summary>
    [RelayCommand]
    public async Task RenameLuggerAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var name = LuggerNameInput.Trim();
        if (name.Length == 0)
        {
            MessageText = RefusedText;
            return;
        }

        var publicId = SelectedLugger.PublicId;

        await ApplyAsync(
            () => _arrangementService.RenameLuggerAsync(
                publicId,
                name,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Removes the chosen lugger and the pairings that name it (FR-003).</summary>
    [RelayCommand]
    public async Task RemoveLuggerAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var publicId = SelectedLugger.PublicId;

        await ApplyAsync(
            () => _arrangementService.RemoveLuggerAsync(
                publicId,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Sets the chosen lugger's status, refusing a blank reason for <c>Do Not Use</c> before the call (FR-019),
    /// and offering to make a backup available when this change leaves a scrap type with no available lugger.
    /// </summary>
    /// <remarks>
    /// <b>The question is asked after the write, never instead of it.</b> The store accepts the new status whatever
    /// the arrangement looks like afterwards, so the change is not refused and not held back: what the person is
    /// offered is the one action that keeps handlers walking to a lugger for the scrap types this lugger was
    /// holding. Answering no changes nothing further and leaves the responsibility with them, which the editor's
    /// own message states rather than a dialog that goes away (owner decision, 2026-10-26).
    /// </remarks>
    [RelayCommand]
    public async Task SetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var status = LuggerStatus.Parse(StatusInput) ?? LuggerStatus.Available;
        var requiresReason = LuggerStatus.RequiresReason(status);

        if (requiresReason && string.IsNullOrWhiteSpace(ReasonInput))
        {
            MessageText = ReasonRequiredText;
            return;
        }

        var reason = requiresReason ? ReasonInput.Trim() : null;
        var publicId = SelectedLugger.PublicId;
        var luggerName = SelectedLugger.Name;

        // Read before the write: once it lands, the load below reports the new status and the change this asks
        // about would look like no change at all.
        var leavingAvailable = string.Equals(SelectedLugger.Status, LuggerStatus.Available, StringComparison.Ordinal)
            && string.Equals(status, LuggerStatus.Available, StringComparison.Ordinal) is false;

        var backups = leavingAvailable ? BackupsLeavingNoAvailableLugger(publicId) : [];

        var written = await ApplyAsync(
            () => _arrangementService.SetStatusAsync(
                publicId,
                status,
                reason,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);

        if (!written || backups.Count == 0)
        {
            return;
        }

        await OfferBackupsAsync(luggerName, backups, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Asks, one lugger at a time, whether each backup that is now the only other lugger for a scrap type should
    /// be made available, and makes the ones the person chose available.
    /// </summary>
    /// <param name="changedLuggerName">The lugger whose status change prompted the question.</param>
    /// <param name="backups">The backups to offer, in the order they were found.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    private async Task OfferBackupsAsync(
        string changedLuggerName,
        IReadOnlyList<(string PublicId, string Name, string ScrapType)> backups,
        CancellationToken cancellationToken)
    {
        foreach (var backup in backups)
        {
            var makeAvailable = _backupPrompt is not null
                && await _backupPrompt
                    .AskToMakeBackupAvailableAsync(changedLuggerName, backup.ScrapType, backup.Name, cancellationToken)
                    .ConfigureAwait(true);

            if (makeAvailable is false)
            {
                // Said in the editor because that is where the person is: the arrangement itself is unchanged, so
                // a line that disappeared with the dialog would leave nothing on screen about it.
                MessageText = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Localize(
                        "Settings_Lugger_Arrangement.BackupLeftAsBackup",
                        "{0} is still a backup for {1}. No lugger is available for that scrap, so setting one is now your responsibility."),
                    backup.Name,
                    backup.ScrapType);

                continue;
            }

            var promoted = await ApplyAsync(
                () => _arrangementService.SetStatusAsync(
                    backup.PublicId,
                    LuggerStatus.Available,
                    null,
                    ActorEmployeeNumber(),
                    ActorEmployeeName(),
                    cancellationToken),
                cancellationToken).ConfigureAwait(true);

            if (promoted)
            {
                MessageText = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Localize("Settings_Lugger_Arrangement.BackupPromoted", "{0} is now available for {1}."),
                    backup.Name,
                    backup.ScrapType);
            }
        }
    }

    /// <summary>
    /// The shipped wording for a key, or the caller's sentence when the key resolves to nothing.
    /// </summary>
    /// <remarks>
    /// The fallback exists so a message this screen composes itself is still a sentence when resources cannot be
    /// read — which is what a headless host sees — rather than a resource key shown where an answer belongs.
    /// </remarks>
    private static string Localize(string key, string fallback)
    {
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }

    /// <summary>
    /// The backup luggers that would be left holding a scrap type on their own once this lugger stops being
    /// available: one per scrap type the lugger takes, in that type's own try order.
    /// </summary>
    /// <param name="changedLuggerPublicId">The lugger whose status is changing.</param>
    /// <returns>One entry per backup lugger, naming the scrap type it is the last other lugger for.</returns>
    /// <remarks>
    /// Only a <c>Backup</c> is offered. A scrap type still held by another <c>Available</c> lugger loses nothing
    /// when this one stops being available, and one whose only other lugger is <c>Full</c> or <c>Do Not Use</c> has
    /// no lugger that could be made available without a person deciding that on its own terms.
    /// </remarks>
    private IReadOnlyList<(string PublicId, string Name, string ScrapType)> BackupsLeavingNoAvailableLugger(
        string changedLuggerPublicId)
    {
        if (_snapshot.Rows.Count == 0)
        {
            return [];
        }

        var mine = _snapshot.Rows
            .Where(row => string.Equals(row.LuggerPublicId, changedLuggerPublicId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(row.ScrapTypeKey))
            .Select(row => row.ScrapTypeKey!)
            .Distinct(StringComparer.Ordinal);

        var offers = new List<(string PublicId, string Name, string ScrapType)>();

        foreach (var key in mine)
        {
            var others = _snapshot.Rows
                .Where(row => string.Equals(row.ScrapTypeKey, key, StringComparison.Ordinal)
                    && string.Equals(row.LuggerPublicId, changedLuggerPublicId, StringComparison.Ordinal) is false
                    && !string.IsNullOrWhiteSpace(row.LuggerPublicId))
                .ToArray();

            if (others.Length == 0
                || others.Any(row => string.Equals(row.LuggerStatus, LuggerStatus.Available, StringComparison.Ordinal)))
            {
                continue;
            }

            var backup = others
                .Where(row => string.Equals(row.LuggerStatus, LuggerStatus.Backup, StringComparison.Ordinal))
                .OrderBy(row => row.SortPosition ?? int.MaxValue)
                .ThenBy(row => row.LuggerName, StringComparer.Ordinal)
                .FirstOrDefault();

            if (backup is null
                || offers.Any(offer => string.Equals(offer.PublicId, backup.LuggerPublicId, StringComparison.Ordinal)))
            {
                continue;
            }

            offers.Add((backup.LuggerPublicId, backup.LuggerName, backup.ScrapType ?? key));
        }

        return offers;
    }

    /// <summary>Gives the chosen scrap type to the chosen lugger, after the luggers it already has (FR-006).</summary>
    [RelayCommand]
    public async Task AssignScrapTypeAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var scrapType = ScrapTypeInput.Trim();
        if (scrapType.Length == 0)
        {
            MessageText = RefusedText;
            return;
        }

        var key = Normalise(scrapType);
        var position = _snapshot.Rows.Count(row => string.Equals(row.ScrapTypeKey, key, StringComparison.Ordinal)) + 1;
        var publicId = SelectedLugger.PublicId;

        await ApplyAsync(
            () => _arrangementService.AssignScrapTypeAsync(
                scrapType,
                publicId,
                position,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Removes the pairing between the chosen scrap type and the chosen lugger (FR-005, FR-013).</summary>
    [RelayCommand]
    public async Task UnassignScrapTypeAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var key = Normalise(ScrapTypeInput);
        if (key.Length == 0)
        {
            MessageText = RefusedText;
            return;
        }

        var publicId = SelectedLugger.PublicId;

        await ApplyAsync(
            () => _arrangementService.UnassignScrapTypeAsync(
                key,
                publicId,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Makes the chosen lugger the dump point for the chosen scrap type (FR-006).</summary>
    [RelayCommand]
    public async Task PromoteLuggerAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || SelectedLugger is null)
        {
            return;
        }

        var key = Normalise(ScrapTypeInput);
        if (key.Length == 0)
        {
            MessageText = RefusedText;
            return;
        }

        var publicId = SelectedLugger.PublicId;

        await ApplyAsync(
            () => _arrangementService.PromoteLuggerAsync(
                publicId,
                key,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Moves a lugger to a place in the display order, which is what a card dragged to a new place asks for
    /// (FR-001). The store renumbers the rest, so the page sends one call rather than a rewrite of every row.
    /// </summary>
    /// <param name="luggerPublicId">The lugger that was moved.</param>
    /// <param name="displayPosition">The 1-based place it was dropped at.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    [RelayCommand]
    public async Task MoveLuggerAsync(
        (string LuggerPublicId, int DisplayPosition) placement,
        CancellationToken cancellationToken = default)
    {
        if (!CanWrite() || string.IsNullOrWhiteSpace(placement.LuggerPublicId))
        {
            return;
        }

        await ApplyAsync(
            () => _arrangementService.SetLuggerPositionAsync(
                placement.LuggerPublicId,
                placement.DisplayPosition,
                ActorEmployeeNumber(),
                ActorEmployeeName(),
                cancellationToken),
            cancellationToken).ConfigureAwait(true);
    }

    partial void OnIsEntitledChanged(bool value) => OnPropertyChanged(nameof(NotEntitled));

    partial void OnStatusInputChanged(string value) => OnPropertyChanged(nameof(IsReasonRequired));

    partial void OnSelectedLuggerChanged(LuggerArrangementRow? value) => OnPropertyChanged(nameof(CanPromote));

    partial void OnScrapTypeInputChanged(string value) => OnPropertyChanged(nameof(CanPromote));

    /// <summary>
    /// Runs one write and reports what happened: a refusal in the store's own words, nothing matched as refused,
    /// and any other failure as that change not having been saved. A write that landed is followed by a fresh read,
    /// so the summary shows the change at once rather than the state the page was drawn from.
    /// </summary>
    /// <remarks>
    /// A failed write leaves the arrangement the page is already showing where it is. Blanking it and saying it
    /// “could not be read” reports a write fault as a read fault and takes away the very list the reader would use
    /// to see that nothing changed — the store still answers the read, so the cards on screen are still true.
    /// </remarks>
    /// <param name="write">The write to run, answering how many rows it matched.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    /// <returns>
    /// <see langword="true"/> when the write landed, so a caller with a follow-on action can tell a saved change
    /// from a refused one.
    /// </returns>
    private async Task<bool> ApplyAsync(Func<Task<int>> write, CancellationToken cancellationToken)
    {
        IsBusy = true;
        MessageText = string.Empty;

        try
        {
            var affectedRows = await write().ConfigureAwait(true);

            if (affectedRows == 0)
            {
                MessageText = RefusedText;
                return false;
            }

            await LoadArrangementAsync(cancellationToken).ConfigureAwait(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LuggerArrangementRefusalException ex)
        {
            MessageText = ex.Message;
            return false;
        }
        catch (LuggerArrangementStoreOutOfDateException ex)
        {
            AppLog.Error("LuggerArrangement", ex, "A lugger arrangement change could not be saved because the store is behind this build.");

            MessageText = StoreOutOfDateText;
            return false;
        }
        catch (Exception ex)
        {
            ShowWriteFailure(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// The picture for one lugger, or <see langword="null"/> when it has none or no resolver is wired.
    /// </summary>
    /// <remarks>
    /// A failure resolves to no picture rather than to a failed load: the arrangement is a working screen, and a
    /// share that cannot be read must not stop a lead from moving a lugger.
    /// </remarks>
    private async Task<string?> ResolveImageAsync(int displayPosition, CancellationToken cancellationToken)
    {
        if (_luggerImages is null)
        {
            return null;
        }

        try
        {
            return await _luggerImages.ResolveAsync(displayPosition, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "LuggerArrangement",
                ex,
                $"The picture for lugger {displayPosition} could not be resolved; its card draws the no-picture image.");
            return null;
        }
    }

    /// <summary>The two reads that make the summary, applied without keeping anything beyond this load.</summary>
    private async Task LoadArrangementAsync(CancellationToken cancellationToken)
    {        var luggers = await _arrangementService.GetLuggersAsync(cancellationToken).ConfigureAwait(true);
        var snapshot = await _arrangementService.GetSnapshotAsync(cancellationToken).ConfigureAwait(true);

        _snapshot = snapshot;
        IsStoreUnavailable = false;

        var selectedPublicId = SelectedLugger?.PublicId;

        Luggers.Clear();
        foreach (var lugger in luggers.OrderBy(candidate => candidate.DisplayPosition))
        {
            // The card draws the picture at half size, so it is read at half size too. The width is taken from the
            // file's header; when the lugger has no picture the same question is asked of the placeholder, which is
            // what the card falls back to.
            var imagePath = await ResolveImageAsync(lugger.DisplayPosition, cancellationToken).ConfigureAwait(true);
            var pixelWidth = await LuggerImageResolver.ReadPixelWidthAsync(
                string.IsNullOrWhiteSpace(imagePath) ? ImagePicturePolicy.NoImagePath : imagePath)
                .ConfigureAwait(true);

            Luggers.Add(new LuggerArrangementRow
            {
                PublicId = lugger.PublicId,
                Name = lugger.Name,
                Status = lugger.Status,
                StatusReason = lugger.StatusReason,
                DisplayPosition = lugger.DisplayPosition,
                ScrapTypes = ScrapTypesFor(snapshot, lugger.PublicId),
                ImagePath = imagePath,
                ImagePixelWidth = pixelWidth,
            });
        }

        ScrapTypes.Clear();
        foreach (var scrapType in snapshot.ScrapTypes)
        {
            ScrapTypes.Add(new ScrapTypeDumpPointRow
            {
                ScrapType = scrapType,
                ScrapTypeKey = Normalise(scrapType),
                DumpPointName = snapshot.Resolve(scrapType).LuggerLocation,
            });
        }

        // The picker's choices are rebuilt on every load, and clearing the bound collection clears the combo box's
        // selection with it. The reader's choice is remembered and put back, so one action does not empty the field
        // the next action needs.
        var chosenScrapType = ScrapTypeInput;

        ScrapTypeOptions.Clear();
        foreach (var option in BuildScrapTypeOptions(snapshot))
        {
            ScrapTypeOptions.Add(option);
        }

        ScrapTypeInput = ScrapTypeOptions.FirstOrDefault(option =>
            string.Equals(option, chosenScrapType, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        SelectedLugger = selectedPublicId is null
            ? Luggers.FirstOrDefault()
            : Luggers.FirstOrDefault(row => string.Equals(row.PublicId, selectedPublicId, StringComparison.Ordinal))
                ?? Luggers.FirstOrDefault();

        // A write can change what this answers while neither the chosen lugger nor the chosen scrap type moved — an
        // assignment added beside the selected one is exactly that — so the answer is restated after every load.
        OnPropertyChanged(nameof(CanPromote));
    }

    /// <summary>The scrap types one lugger takes, in the order that type tries its luggers.</summary>
    private static IReadOnlyList<string> ScrapTypesFor(LuggerArrangementSnapshot snapshot, string luggerPublicId) =>
        snapshot.Rows
            .Where(row => string.Equals(row.LuggerPublicId, luggerPublicId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(row.ScrapType))
            .OrderBy(row => row.SortPosition ?? int.MaxValue)
            .Select(row => row.ScrapType!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// The picker's choices: the shipped catalogue in its own order, then every assigned type it does not name. The
    /// two values that answer whether there is scrap at all are left out, because neither is a place to put it.
    /// </summary>
    private static IEnumerable<string> BuildScrapTypeOptions(LuggerArrangementSnapshot snapshot)
    {
        var options = new List<string>();

        foreach (var shipped in ScrapTypeCatalog.All)
        {
            options.Add(shipped);
        }

        foreach (var assigned in snapshot.ScrapTypes)
        {
            if (string.IsNullOrWhiteSpace(assigned)
                || ScrapDecisionRules.IsRequiredPlaceholder(assigned)
                || ScrapDecisionRules.IsNoScrap(assigned)
                || options.Contains(assigned, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            options.Add(assigned);
        }

        return options;
    }

    /// <summary>The store's matching key: the spelling trimmed and lower-cased, and nothing else (FR-009).</summary>
    private static string Normalise(string? scrapType) =>
        scrapType?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>States the unavailable arrangement and drops what was drawn, so no stale line is left standing in.</summary>
    private void ShowUnavailable(Exception exception)
    {
        AppLog.Error(
            "LuggerArrangement",
            exception,
            "The lugger arrangement could not be read or written, so the page shows its unavailable state rather than a stale or invented arrangement.");

        IsStoreUnavailable = true;
        MessageText = UnavailableText;
        ClearArrangement();
    }

    /// <summary>
    /// States a write that did not land and leaves the arrangement on screen alone: the store still answers the
    /// read, so the cards the reader can see are still the store's own, and taking them away would hide the change
    /// that did not happen behind an empty page.
    /// </summary>
    private void ShowWriteFailure(Exception exception)
    {
        AppLog.Error("LuggerArrangement", exception, "A lugger arrangement change could not be saved.");

        MessageText = WriteFailedText;
    }

    private void ClearArrangement()
    {
        _snapshot = LuggerArrangementSnapshot.Empty;
        Luggers.Clear();
        ScrapTypes.Clear();
        ScrapTypeOptions.Clear();
        SelectedLugger = null;
    }

    /// <summary>The employee number the store records, falling back to the sign-in name when the store holds none.</summary>
    private string ActorEmployeeNumber() =>
        string.IsNullOrWhiteSpace(_personIdentity.EmployeeNumber)
            ? _personIdentity.SignInName
            : _personIdentity.EmployeeNumber!;

    /// <summary>The name the change reads with, so a notice names the person rather than a number (FR-032).</summary>
    private string ActorEmployeeName() => _personIdentity.DisplayName;
}
