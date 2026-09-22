using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The permissions page's behaviour (T059, FR-063 to FR-074, FR-114, SC-010).
/// </summary>
/// <remarks>
/// <para>
/// What reaches the store is proved against the live store in T058. What this suite proves is the page: how the
/// matrix is drawn from the declaration, what the three cell states are and that each one says itself in words,
/// what is pending, what the reader is asked to confirm, what the undo does, and what a person who outranks the
/// reader looks like.
/// </para>
/// <para>
/// <b>The double enforces the store's own guard on purpose.</b> A change set carries the value the page last saw
/// as its `from`, and the store refuses a write whose `from` is not what it holds — including a `from` that is not
/// null when the person has no stored value of their own. A double that accepted anything hid exactly the defect
/// that made every first-time save from this page fail against the live store, so the double refuses the same
/// things the procedure refuses.
/// </para>
/// </remarks>
[TestClass]
public sealed class PermissionsViewModelTests
{
    private const long SignedInUserId = 42;
    private const long TargetUserId = 77;
    private const long SecondUserId = 78;

    /// <summary>Two people: one at the reader's own rung and one above it, which is what most tests need.</summary>
    private static FakeUserManagementService TwoPeople() =>
        new((TargetUserId, "Tess Target", "TTARGET", "setup"), (SecondUserId, "Ruby Lead", "RLEAD", "setup_lead"));

    [TestMethod]
    public async Task TheFiveCards_AreTheDeclarationAreas_AndEveryPermissionIsInExactlyOne()
    {
        var viewModel = Build();

        await viewModel.InitializeAsync();

        var expected = Enum.GetValues<PermissionRegistry.Area>();
        Assert.AreEqual(5, viewModel.Cards.Count, "The declaration names five areas, so the page shows five cards.");
        CollectionAssert.AreEqual(
            expected.Select(area => $"Permissions_Area_{area}.Heading".GetLocalized()).ToList(),
            viewModel.Cards.Select(card => card.AreaHeadingText).ToList(),
            "The cards are the areas in the declaration's own order.");

        CollectionAssert.AreEquivalent(
            PermissionRegistry.All.Select(entry => entry.Key).ToList(),
            viewModel.Cards.SelectMany(card => card.Columns.Select(column => column.Key)).ToList(),
            "Every declared permission appears in its area's card, and none appears twice (FR-047).");

        var settings = viewModel.Cards.Single(card => card.Columns.Any(column => column.Key == PermissionKeys.SettingsHotWorkCenters));
        Assert.AreEqual(8, settings.Columns.Count, "The largest card carries eight permissions.");
    }

    [TestMethod]
    public async Task TheColumns_CarryThePermissionsWords_AndNeverItsKey()
    {
        var viewModel = Build();

        await viewModel.InitializeAsync();

        foreach (var column in viewModel.Cards.SelectMany(card => card.Columns))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.LabelText), "A column says what the permission is.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.GatesText), "And what it gates (FR-065).");
            Assert.AreNotEqual(column.Key, column.LabelText, "A permission's key is never what a person reads.");
            Assert.IsFalse(
                column.LabelText.EndsWith(".Label", StringComparison.Ordinal),
                "A resource key is never what a person reads.");
        }
    }

    [TestMethod]
    public async Task TheRows_AreThePeople_OrderedByRungAndThenByName()
    {
        var people = new FakeUserManagementService(
            (1, "Zoe Worker", "ZWORKER", "setup"),
            (2, "Bob Manager", "BMANAGER", "plant_manager"),
            (3, "Ann Worker", "AWORKER", "setup"));

        var viewModel = Build(people: people);

        await viewModel.InitializeAsync();

        CollectionAssert.AreEqual(
            new[] { "Bob Manager", "Ann Worker", "Zoe Worker" },
            viewModel.Rows.Select(row => row.DisplayName).ToList(),
            "The highest rung comes first and one rung's own people are in name order.");
    }

    [TestMethod]
    public async Task TwoAccountsSharingAName_AreStillToldApart()
    {
        var people = new FakeUserManagementService(
            (1, "John Koll", "JOHNK", "setup"),
            (2, "John Koll", "JKOLL", "setup"));

        var viewModel = Build(people: people);

        await viewModel.InitializeAsync();

        Assert.AreEqual(2, viewModel.Rows.Count, "Two accounts are two rows, whatever they are called.");
        CollectionAssert.AreEqual(
            new[] { "JKOLL", "JOHNK" },
            viewModel.Rows.Select(row => row.SignInName).ToList(),
            "And the sign-in name is the tie-break, so the two rows cannot swap places between loads.");
        Assert.AreNotEqual(
            Cell(viewModel.Rows[0], PermissionKeys.SetupWorkCenters).PersonName,
            Cell(viewModel.Rows[1], PermissionKeys.SetupWorkCenters).PersonName,
            "A cell read on its own names the account, not only the person.");
    }

    [TestMethod]
    public async Task EveryPersonAppearsInEveryCard_WithOnlyThatCardsCells()
    {
        var viewModel = Build();

        await viewModel.InitializeAsync();

        foreach (var card in viewModel.Cards)
        {
            Assert.AreEqual(viewModel.Rows.Count, card.Rows.Count, "One row per person on every card.");
            foreach (var row in card.Rows)
            {
                CollectionAssert.AreEquivalent(
                    card.Columns.Select(column => column.Key).ToList(),
                    row.Cells.Select(cell => cell.Key).ToList(),
                    "A card's row carries that card's columns and nothing else.");
            }
        }
    }

    [TestMethod]
    public async Task AChosenCell_AnInheritedCell_AndAnOffCell_AreThreeDifferentShapes()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsIgnoredLocations, true)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsPartPictures, false);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        var chosen = Cell(row, PermissionKeys.SettingsHotWorkCenters);
        var inherited = Cell(row, PermissionKeys.SettingsIgnoredLocations);
        var off = Cell(row, PermissionKeys.SettingsPartPictures);

        Assert.IsTrue(chosen.IsChosenShape, "A value chosen for this person is one shape.");
        Assert.IsTrue(inherited.IsInheritedShape, "A value their role's baseline gives is another.");
        Assert.IsTrue(off.IsOffShape, "A value nobody allows is the third.");

        Assert.IsFalse(chosen.IsInheritedShape);
        Assert.IsFalse(inherited.IsChosenShape);
        Assert.IsFalse(off.IsChosenShape);
        Assert.IsFalse(off.IsInheritedShape, "The three shapes are never two at once.");
    }

    [TestMethod]
    public async Task EveryCell_SaysItsStateAndWhoseItIsInWords_BecauseAShapeCannotBeReadAloud()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsIgnoredLocations, true)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsPartPictures, false);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        var chosen = Cell(row, PermissionKeys.SettingsHotWorkCenters);
        var inherited = Cell(row, PermissionKeys.SettingsIgnoredLocations);
        var off = Cell(row, PermissionKeys.SettingsPartPictures);

        // The test host has no resource map, so the lookup answers with the key and substitutes nothing. What can
        // be proved here is that every cell says something, that the three states say three different things, and
        // that a cell which is not saved yet says so on top of that. The sentences themselves are checked in the
        // shipped resource file, and both facts each of them must carry are proved to be in it.
        foreach (var cell in new[] { chosen, inherited, off })
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(cell.Announcement), "Every cell announces something.");
        }

        Assert.AreNotEqual(
            chosen.Announcement,
            inherited.Announcement,
            "Two cells that look different must also be described differently (FR-065).");
        Assert.AreNotEqual(chosen.Announcement, off.Announcement);
        Assert.AreNotEqual(inherited.Announcement, off.Announcement);

        var beforeItIsTouched = chosen.Announcement;
        chosen.ToggleCommand.Execute(null);
        Assert.AreNotEqual(
            beforeItIsTouched,
            chosen.Announcement,
            "A cell that is not saved yet says so on top of its state.");

        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.OnChosen"), "{0}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.OnChosen"), "{1}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.OnInherited"), "{0}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.Off"), "{0}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.Fixed"), "cannot be changed");
        StringAssert.Contains(ShippedResourceValue("Permissions_Cell.Locked"), "cannot be changed");
    }

    [TestMethod]
    public async Task TurningACellOver_MarksTheRowPending_AndTheWarningStatesTheCount()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true));

        await viewModel.InitializeAsync();

        Assert.AreEqual(0, viewModel.PendingCount);
        Assert.IsFalse(viewModel.HasPendingChanges);
        Assert.IsTrue(viewModel.NothingChanged, "Nothing changed is a state the page states (FR-068).");
        Assert.AreEqual(0, viewModel.PendingRows.Count, "Nothing pending means nobody in the unsaved list.");

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        Assert.IsTrue(Cell(row, PermissionKeys.SettingsHotWorkCenters).IsPending, "A turned-over cell is pending (FR-074).");
        Assert.IsTrue(Cell(row, PermissionKeys.SettingsHotWorkCenters).IsPendingShape);
        Assert.AreEqual(1, viewModel.PendingCount);
        Assert.IsTrue(viewModel.HasPendingChanges);
        Assert.AreEqual(1, viewModel.PendingRows.Count, "The person with something pending is listed once.");
        Assert.IsFalse(viewModel.NothingChanged);

        // One pending change is said in the singular; several in the plural (FR-073).
        var one = viewModel.UnsavedWarningText;
        Assert.IsTrue(
            one.Contains("1", StringComparison.Ordinal) || one.Contains("LeavingOne", StringComparison.Ordinal),
            $"One pending change is not read as \"1 changes\": {one}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Unsaved.Leaving"), "{0}");
    }

    [TestMethod]
    public async Task TheConfirmation_NamesWhatChangesAndForWhom_AndCountsWhenSeveralChange()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsIgnoredLocations, false));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();

        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        var single = row.ConfirmationSentences;
        Assert.AreEqual(1, single.Count, "One changed cell is one sentence.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(single[0]));
        Assert.AreEqual(
            row.CurrentChangeSet.Entries.Count,
            single.Count,
            "One sentence per changed cell, so what is confirmed and what is written are the same set.");

        Cell(row, PermissionKeys.SettingsIgnoredLocations).ToggleCommand.Execute(null);

        var several = row.ConfirmationSentences;
        Assert.AreEqual(3, several.Count, "Several changes give a count and a sentence per change (FR-067).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(several[0]));

        // The sentences' own wording is read from the resource file rather than from the lookup: the test host has
        // no resource map, so a lookup answers with the key and substitutes no placeholder. Both facts the reader
        // is told are proved to be in the sentence.
        StringAssert.Contains(ShippedResourceValue("Permissions_Save.Confirmation"), "{0}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Save.Confirmation"), "{1}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Save.ConfirmationCount"), "{0}");
        StringAssert.Contains(ShippedResourceValue("Permissions_Save.ConfirmationCount"), "{1}");

        Assert.AreEqual(Cell(row, PermissionKeys.SettingsHotWorkCenters).ChangeSentence, several[1]);
        Assert.AreEqual(Cell(row, PermissionKeys.SettingsIgnoredLocations).ChangeSentence, several[2]);
    }

    [TestMethod]
    public async Task TheChangeSet_SendsNoValueFromThePerson_WhenTheirRoleSuppliesTheAnswer()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        var entry = row.CurrentChangeSet.Entries.Single();

        Assert.IsNull(
            entry.From,
            "A value the person does not own is not the value the store holds for them, so the `from` is null (FR-070).");
        Assert.IsTrue(entry.To is false);
        Assert.AreEqual(TargetUserId, row.CurrentChangeSet.UserId, "A change set is written for one person.");
    }

    [TestMethod]
    public async Task TheChangeSet_SendsTheStoredValueBack_WhenThePersonOwnsIt()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        var entry = row.CurrentChangeSet.Entries.Single();

        Assert.IsTrue(entry.From is true, "A value the person owns travels with the change, so a move is refused (FR-070).");
        Assert.IsTrue(entry.To is false);
    }

    [TestMethod]
    public async Task ASaveOfACellThePersonDoesNotOwn_Lands()
    {
        var service = new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        await viewModel.SaveAsync(row);

        Assert.AreEqual(1, service.Applies, "A first-time change is written rather than refused as a value that moved.");
        Assert.AreEqual(0, row.PendingCount);
        Assert.IsTrue(Cell(row, PermissionKeys.SettingsHotWorkCenters).IsOn, "And what the store read back is what the page shows.");
        Assert.IsTrue(Cell(row, PermissionKeys.SettingsHotWorkCenters).IsChosenShape);
    }

    [TestMethod]
    public async Task OneImpatientPress_WritesExactlyOneChange()
    {
        var service = new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true);
        service.SaveGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        var first = viewModel.SaveAsync(row);
        await viewModel.SaveAsync(row);

        Assert.AreEqual(1, service.Applies, "A second press while a save is in flight is the same press (SC-010).");

        service.ReleaseSave();
        await first;
    }

    [TestMethod]
    public async Task ASave_WritesOnePersonsWholeSet_AndAnotherPersonIsAnotherWrite()
    {
        var service = new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false)
            .SetFromRole(TargetUserId, PermissionKeys.SettingsIgnoredLocations, false)
            .SetFromRole(SecondUserId, PermissionKeys.SettingsHotWorkCenters, false);

        var viewModel = Build(service: service, people: TwoPeople());

        await viewModel.InitializeAsync();

        var first = viewModel.Rows.Single(row => row.UserId == TargetUserId);
        var second = viewModel.Rows.Single(row => row.UserId == SecondUserId);

        Cell(first, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);
        Cell(first, PermissionKeys.SettingsIgnoredLocations).ToggleCommand.Execute(null);
        Cell(second, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        Assert.AreEqual(3, viewModel.PendingCount, "Three cells across two people are three pending changes.");
        Assert.AreEqual(2, viewModel.PendingRows.Count, "And two people to save.");

        await viewModel.SaveAsync(first);

        Assert.AreEqual(1, service.Applies, "One save for one person is one write (FR-071).");
        Assert.AreEqual(TargetUserId, service.LastUserId);
        Assert.AreEqual(2, service.LastChangeCount, "And it carried the whole of that person's set.");

        await viewModel.SaveAsync(second);

        Assert.AreEqual(2, service.Applies, "The other person is a second write, because a write is one person's.");
    }

    [TestMethod]
    public async Task ASaveThatLanded_ShowsWhatTheStoreHolds_AndStopsTheRowBeingPending()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        var cell = Cell(row, PermissionKeys.SettingsHotWorkCenters);
        cell.ToggleCommand.Execute(null);

        await viewModel.SaveAsync(row);

        Assert.AreEqual(0, row.PendingCount, "After the save nothing is pending.");
        Assert.IsFalse(cell.IsPending);
        Assert.IsFalse(cell.IsOn, "The page shows what the store read back.");
        Assert.IsTrue(cell.IsOffShape);

        // Turning it on again shows the value the store now holds as this person's own rather than their role's,
        // which is what the save wrote and what the read-back brought back.
        cell.ToggleCommand.Execute(null);
        Assert.IsTrue(cell.IsChosenShape, "A value written for this person is drawn as theirs, not as their role's.");
        Assert.IsFalse(cell.IsInheritedShape);

        StringAssert.Contains(ShippedResourceValue("Permissions_Save.SavedFor"), "{0}", "The saved message names the person.");
    }

    [TestMethod]
    public async Task ASaveThatLanded_AnnouncesTheCellsNewState_NotOnlyItsNewValue()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        var cell = Cell(row, PermissionKeys.SettingsHotWorkCenters);
        cell.ToggleCommand.Execute(null);

        var announced = new List<string>();
        cell.PropertyChanged += (_, args) => announced.Add(args.PropertyName ?? string.Empty);

        await viewModel.SaveAsync(row);

        // The value on screen did not move: the save wrote what the reader had already chosen. What changed is the
        // state, so the state is what has to be announced or the screen keeps saying "not saved yet" over a cell
        // that has just been written.
        Assert.IsTrue(cell.IsOn);
        Assert.IsFalse(cell.IsPending, "The cell is no longer pending.");
        Assert.IsTrue(
            announced.Contains(nameof(PermissionCell.Announcement)),
            "A re-based cell announces its state, so nothing on screen keeps a stale answer.");
        Assert.IsTrue(announced.Contains(nameof(PermissionCell.IsPendingShape)));
        Assert.IsTrue(announced.Contains(nameof(PermissionCell.IsChosenShape)));
    }

    [TestMethod]
    public async Task AValueThatMovedWhileThePageWasOpen_IsNamedAndNothingIsWritten()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, true);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        // Somebody else changes the same value between the page reading it and the save writing it.
        service.SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false);

        await viewModel.SaveAsync(row);

        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.MessageText), "The reader is told what happened (FR-070).");
        Assert.AreEqual(
            PermissionChangeOutcomeKind.ValueMoved,
            service.LastRefusal,
            "The store's refusal is what the page reports, rather than a change that never landed.");
        Assert.AreEqual(0, row.PendingCount, "And the row is re-based on what the store now holds.");
        StringAssert.Contains(
            ShippedResourceValue("Permissions_Save.ValueMoved"),
            "{0}",
            "The sentence names the permission somebody else changed.");
        StringAssert.Contains(
            ShippedResourceValue("Permissions_Save.ValueMoved"),
            "{1}",
            "And whose permission it is.");
    }

    [TestMethod]
    public async Task TheUndo_ReversesTheWholeSave_ForThePersonItWasWrittenFor()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false)
            .SetOwn(TargetUserId, PermissionKeys.SettingsIgnoredLocations, true);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.CanUndo, "There is nothing to undo before anything has been saved.");

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);
        await viewModel.SaveAsync(row);

        Assert.IsTrue(viewModel.CanUndo, "A completed save can be undone (FR-069).");

        await viewModel.UndoAsync();

        Assert.AreEqual(1, service.Reversals, "One undo is one call, which is what makes it reverse the whole save (FR-071).");
        Assert.AreEqual(TargetUserId, service.LastReversalUserId, "And it is for the person the save was written for.");
        Assert.IsFalse(service.LastReversalOverrodeMovedValue, "The first attempt never overrides a moved value.");
    }

    [TestMethod]
    public async Task AValueThatMoved_IsShownAndAskedAbout_BeforeItIsRestored()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false);
        service.ReversalResult = PermissionChangeResult.Failed(
            PermissionChangeOutcomeKind.ValueMoved,
            PermissionAdministrationMessages.ValueMovedKey,
            PermissionAdministrationMessages.ValueMoved,
            PermissionKeys.SettingsHotWorkCenters);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);
        await viewModel.SaveAsync(row);

        await viewModel.UndoAsync();

        Assert.IsTrue(viewModel.IsRestorePromptVisible, "The page asks before restoring a value that has moved (FR-070).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.RestorePromptText), "It says what the value is now.");
        StringAssert.Contains(
            ShippedResourceValue("Permissions_Undo.ValueMoved"),
            "{0}",
            "And it names the permission somebody else changed.");
        StringAssert.Contains(
            ShippedResourceValue("Permissions_Undo.ValueMoved"),
            "?",
            "It asks, rather than putting the value back.");
        Assert.AreEqual(1, service.Reversals, "Nothing was written: the undo stopped and asked.");

        service.ReversalResult = PermissionChangeResult.Succeeded();
        await viewModel.ConfirmRestoreAsync();

        Assert.AreEqual(2, service.Reversals);
        Assert.IsTrue(service.LastReversalOverrodeMovedValue, "The reader's answer is what allows the restore.");
        Assert.IsFalse(viewModel.IsRestorePromptVisible);
    }

    [TestMethod]
    public async Task DecliningTheRestore_LeavesTheValueWhereItIs()
    {
        var service = new FakePermissionAdministrationService()
            .SetOwn(TargetUserId, PermissionKeys.SettingsHotWorkCenters, false);
        service.ReversalResult = PermissionChangeResult.Failed(
            PermissionChangeOutcomeKind.ValueMoved,
            PermissionAdministrationMessages.ValueMovedKey,
            PermissionAdministrationMessages.ValueMoved,
            PermissionKeys.SettingsHotWorkCenters);

        var viewModel = Build(service: service);

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);
        await viewModel.SaveAsync(row);
        await viewModel.UndoAsync();

        viewModel.DismissRestore();

        Assert.IsFalse(viewModel.IsRestorePromptVisible);
        Assert.AreEqual(1, service.Reversals, "Declining writes nothing at all.");
    }

    [TestMethod]
    public async Task APersonAboveTheReadersRung_HasEveryCellLocked_WithTheReasonInWords()
    {
        var viewModel = Build(people: TwoPeople(), readerRoleCode: "setup");

        await viewModel.InitializeAsync();

        var above = viewModel.Rows.Single(row => row.UserId == SecondUserId);

        Assert.IsTrue(above.IsLocked, "Their role is above the reader's (FR-066).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(above.LockReasonText), "The reason is stated in words.");
        Assert.IsTrue(viewModel.HasLockedRows, "And the page says so above the matrix.");
        Assert.IsTrue(above.Cells.All(cell => cell.IsLocked), "Their whole row is locked.");
        Assert.IsTrue(above.Cells.All(cell => !cell.CanEdit));
        Assert.IsTrue(
            above.Cells.Where(cell => !cell.IsFixed).All(cell => cell.Announcement.Contains("Permissions_Cell.Locked".GetLocalized(), StringComparison.Ordinal)),
            "Every cell of theirs says why it cannot be changed; only the page's own permission says its own reason.");
        Assert.IsFalse(above.CanSave, "And there is nothing to save for them.");

        // Turning a cell over is not offered, and even if it were the cell does not move.
        var cell = Cell(above, PermissionKeys.SetupWorkCenters);
        cell.ToggleCommand.Execute(null);

        Assert.IsFalse(cell.IsPending, "A locked cell does not move, so nobody is offered a change that cannot land.");
        Assert.AreEqual(0, viewModel.PendingCount);

        var atTheReadersRung = viewModel.Rows.Single(row => row.UserId == TargetUserId);
        Assert.IsFalse(atTheReadersRung.IsLocked, "A peer is not above the reader.");
    }

    [TestMethod]
    public async Task TheFixedCell_IsPresent_Locked_WithItsReason_AndClearableByNobody()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService()
            .SetFromRole(TargetUserId, PermissionKeys.AdminPermissions, true));

        await viewModel.InitializeAsync();

        var row = viewModel.Rows.Single();
        var fixedCell = Cell(row, PermissionKeys.AdminPermissions);

        Assert.IsTrue(fixedCell.IsFixed, "The permission that opens this page is present on it (FR-059).");
        Assert.IsTrue(fixedCell.IsLocked);
        Assert.IsFalse(fixedCell.CanEdit);
        StringAssert.Contains(fixedCell.Announcement, "Permissions_Cell.Fixed".GetLocalized(), "And it says why, in words.");

        // Changing something else does not put it in the set, because the store refuses it as well.
        Cell(row, PermissionKeys.SettingsHotWorkCenters).ToggleCommand.Execute(null);

        var changes = row.CurrentChangeSet.Entries;
        Assert.AreEqual(1, changes.Count, "Only the cell that can change is in the set.");
        Assert.AreNotEqual(PermissionKeys.AdminPermissions, changes[0].Key);
    }

    [TestMethod]
    public async Task AReaderWithoutThePermission_IsToldSoRatherThanShownAWorkingScreen()
    {
        var viewModel = Build(entitled: false);

        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.IsEntitled);
        Assert.IsTrue(viewModel.NotEntitled);
        Assert.IsFalse(viewModel.CanSave);
        Assert.IsFalse(viewModel.CanUndo);
        Assert.AreEqual(0, viewModel.Cards.Count, "And no matrix is drawn for them.");
    }

    [TestMethod]
    public async Task AnUnreachableStore_IsStatedRatherThanShownAsAnEmptyMatrix()
    {
        var people = TwoPeople();
        people.ThrowOnPeopleRead = new InvalidOperationException("down");

        var viewModel = Build(people: people);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsStoreUnavailable);
        Assert.AreEqual(0, viewModel.Rows.Count, "No sample row takes the matrix's place.");
        Assert.IsFalse(viewModel.IsRosterEmpty, "A store that failed is not the same as there being nobody (FR-096).");
        Assert.AreEqual(viewModel.UnavailableText, viewModel.MessageText);
        Assert.IsFalse(viewModel.IsBusy, "The screen does not freeze or refuse interaction while it loads (FR-114).");
    }

    [TestMethod]
    public async Task NobodyToShow_IsStatedDistinctlyFromAStoreThatFailed()
    {
        var viewModel = Build(people: new FakeUserManagementService());

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsRosterEmpty);
        Assert.IsFalse(viewModel.IsStoreUnavailable, "There being nobody is not a failure.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.EmptyText));
    }

    /// <summary>A cell of a row, by the permission it is about.</summary>
    private static PermissionCell Cell(PermissionMatrixRow row, string permissionKey) =>
        row.Cells.Single(cell => string.Equals(cell.Key, permissionKey, StringComparison.Ordinal));

    /// <summary>The shipped value of a resource key, read from the resource file rather than from the lookup.</summary>
    private static string ShippedResourceValue(string resourceKey)
    {
        var path = Path.Combine(
            MTM_Waitlist.Tests.Module_Mock.RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw");

        Assert.IsTrue(File.Exists(path), $"The resource file was not found at '{path}'.");

        var entry = System.Xml.Linq.XDocument.Load(path)
            .Descendants("data")
            .FirstOrDefault(data => data.Attribute("name")?.Value == resourceKey);

        Assert.IsNotNull(entry, $"'{resourceKey}' has no entry, so its sentence would never be shown.");

        return entry!.Element("value")?.Value ?? string.Empty;
    }

    private static PermissionsViewModel Build(
        FakePermissionAdministrationService? service = null,
        FakeUserManagementService? people = null,
        bool entitled = true,
        string readerRoleCode = "developer")
    {
        var state = new StartupState
        {
            UserId = SignedInUserId,
            Username = "JSMITH",
            EmployeeName = "Jane Smith",
            CurrentRoleCode = readerRoleCode,
        };

        return new PermissionsViewModel(
            service ?? new FakePermissionAdministrationService(),
            PermissionStub.Holding(entitled ? [PermissionKeys.AdminPermissions] : []),
            people ?? new FakeUserManagementService((TargetUserId, "Tess Target", "TTARGET", "setup")),
            new CatalogueStub(),
            new RecordingNavigationService(),
            state);
    }

    /// <summary>
    /// The store, as far as this page can tell: which values a person owns, which their role supplies, and the
    /// same refusals the change-set procedure raises.
    /// </summary>
    private sealed class FakePermissionAdministrationService : IPermissionAdministrationService
    {
        private readonly Dictionary<long, Dictionary<string, bool>> _owned = new();
        private readonly Dictionary<long, Dictionary<string, bool>> _fromRole = new();

        /// <summary>Gives the person a stored value of their own, which is what makes a `from` non-null.</summary>
        internal FakePermissionAdministrationService SetOwn(long userId, string key, bool value)
        {
            Owned(userId)[key] = value;
            FromRole(userId).Remove(key);
            return this;
        }

        /// <summary>Leaves the person with no stored value, so their role supplies the answer.</summary>
        internal FakePermissionAdministrationService SetFromRole(long userId, string key, bool value)
        {
            FromRole(userId)[key] = value;
            Owned(userId).Remove(key);
            return this;
        }

        private Dictionary<string, bool> Owned(long userId)
        {
            if (!_owned.TryGetValue(userId, out var rows))
            {
                rows = new Dictionary<string, bool>(StringComparer.Ordinal);
                _owned[userId] = rows;
            }

            return rows;
        }

        private Dictionary<string, bool> FromRole(long userId)
        {
            if (!_fromRole.TryGetValue(userId, out var rows))
            {
                rows = new Dictionary<string, bool>(StringComparer.Ordinal);
                _fromRole[userId] = rows;
            }

            return rows;
        }

        internal TaskCompletionSource<bool>? SaveGate { get; set; }

        internal PermissionChangeResult ReversalResult { get; set; } = PermissionChangeResult.Succeeded();

        internal int Applies { get; private set; }

        internal int Reversals { get; private set; }

        internal int LastChangeCount { get; private set; }

        internal long LastUserId { get; private set; }

        internal long LastReversalUserId { get; private set; }

        internal bool LastReversalOverrodeMovedValue { get; private set; }

        internal PermissionChangeOutcomeKind? LastRefusal { get; private set; }

        /// <summary>Opens the write gate a test closed, so the first press can finish.</summary>
        internal void ReleaseSave() => SaveGate?.TrySetResult(true);

        public Task<IReadOnlyList<PermissionValueRow>> GetForPersonAsync(long userId, CancellationToken cancellationToken = default)
        {
            var composed = new List<PermissionValueRow>();

            foreach (var entry in Owned(userId))
            {
                composed.Add(new PermissionValueRow(entry.Key, entry.Value, PermissionProvenance.Chosen));
            }

            foreach (var entry in FromRole(userId))
            {
                if (!Owned(userId).ContainsKey(entry.Key))
                {
                    composed.Add(new PermissionValueRow(entry.Key, entry.Value, PermissionProvenance.Inherited));
                }
            }

            return Task.FromResult<IReadOnlyList<PermissionValueRow>>(composed);
        }

        public async Task<PermissionChangeResult> ApplyAsync(
            long userId,
            IReadOnlyList<PermissionChange> changes,
            CancellationToken cancellationToken = default)
        {
            Applies++;
            LastUserId = userId;
            LastChangeCount = changes.Count;

            if (SaveGate is { } gate && !gate.Task.IsCompleted)
            {
                await gate.Task;
            }

            // The guard the procedure applies, applied here too: a `from` that is not what the store holds for
            // this person refuses the whole set and names the key that moved.
            foreach (var change in changes)
            {
                var owns = Owned(userId).TryGetValue(change.Key, out var stored);

                if (change.From is null ? owns : !owns || stored != change.From)
                {
                    LastRefusal = PermissionChangeOutcomeKind.ValueMoved;
                    return PermissionChangeResult.Failed(
                        PermissionChangeOutcomeKind.ValueMoved,
                        PermissionAdministrationMessages.ValueMovedKey,
                        PermissionAdministrationMessages.ValueMoved,
                        change.Key);
                }
            }

            // The whole set lands or none of it does, which is what one call means.
            foreach (var change in changes)
            {
                if (change.To is null)
                {
                    Owned(userId).Remove(change.Key);
                }
                else
                {
                    Owned(userId)[change.Key] = change.To.Value;
                }
            }

            LastRefusal = null;
            return PermissionChangeResult.Succeeded();
        }

        public Task<PermissionChangeResult> ReverseLastSaveAsync(
            long userId,
            bool restoreDespiteMovedValue = false,
            CancellationToken cancellationToken = default)
        {
            Reversals++;
            LastReversalUserId = userId;
            LastReversalOverrodeMovedValue = restoreDespiteMovedValue;
            return Task.FromResult(ReversalResult);
        }

        public Task<PermissionHolders> GetHoldersAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PermissionHolders([], []));
    }

    private sealed class FakeUserManagementService : IUserManagementService
    {
        private readonly IReadOnlyList<UserRosterRow> _people;

        internal FakeUserManagementService(params (long UserId, string DisplayName, string Username, string RoleCode)[] people) =>
            _people = [.. people.Select(person => new UserRosterRow(
                person.UserId,
                $"public-{person.UserId}",
                person.Username,
                person.DisplayName,
                "6229",
                person.RoleCode,
                person.RoleCode,
                0,
                true))];

        /// <summary>When set, the people cannot be read, which is how the unavailable state is reached.</summary>
        internal Exception? ThrowOnPeopleRead { get; set; }

        public Task<IReadOnlyList<UserRosterRow>> SearchAsync(string? searchText, string? roleCode, CancellationToken cancellationToken = default) =>
            ThrowOnPeopleRead is not null
                ? Task.FromException<IReadOnlyList<UserRosterRow>>(ThrowOnPeopleRead)
                : Task.FromResult(_people);

        public Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);

        public Task<UserManagementResult> CreateAsync(UserAccountEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("1234"));

        public Task<UserManagementResult> UpdateAsync(long userId, UserAccountEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded());

        public Task<UserManagementResult> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserManagementResult.Succeeded("5678"));

        public Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CatalogueStub : IRoleCatalogService
    {
        public Task<IReadOnlyList<RoleCatalogEntry>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleCatalogEntry>>(
            [
                new RoleCatalogEntry(1, "developer", "Developer", 100),
                new RoleCatalogEntry(2, "plant_manager", "Plant Manager", 80),
                new RoleCatalogEntry(3, "setup_lead", "Setup Lead", 40),
                new RoleCatalogEntry(4, "setup", "Setup", 10),
            ]);

        public void Invalidate()
        {
        }
    }

    private sealed class PermissionStub : IPermissionService
    {
        private PermissionStub(IEnumerable<string> held) => HeldKeys = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(params string[] heldPermissionKeys) => new(heldPermissionKeys);

        internal HashSet<string> HeldKeys { get; }

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(HeldKeys.Contains(permissionKey));

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, HeldKeys.Contains, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public Microsoft.UI.Xaml.Controls.Frame? Frame
        {
            get => null;
            set { }
        }

        public bool CanGoBack => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
