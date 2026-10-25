using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Tests.Module_Shared.Dialogs;

/// <summary>
/// The six standard questions answer the same way on every screen (014-adopt-lab-design-system, FR-008, FR-009,
/// FR-011).
/// </summary>
/// <remarks>
/// These are the guarantees a screen is allowed to rely on without checking: the order the buttons appear in, which one
/// Enter takes, and what Escape and the window's own close button mean. If any of them drifted, one screen would start
/// asking permission differently from the rest, and nothing would report it.
/// </remarks>
[TestClass]
public sealed class MessageDialogContractTests
{
    /// <summary>The six kinds, spelled out so a new member cannot slip in unaccounted for.</summary>
    private static readonly MessageKind[] s_allKinds =
    [
        MessageKind.Acknowledge,
        MessageKind.Confirm,
        MessageKind.Decide,
        MessageKind.DestructiveConfirm,
        MessageKind.LeaveWithoutSaving,
        MessageKind.Retry,
    ];

    [TestMethod]
    public void Audit_MessageDialogContractTests_ThereAreExactlySix()
    {
        CollectionAssert.AreEquivalent(
            s_allKinds,
            Enum.GetValues<MessageKind>(),
            "the standard set is a closed list of six; a seventh kind is a decision, not an addition");
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_EveryKindLooksUp()
    {
        foreach (var kind in s_allKinds)
        {
            Assert.IsNotNull(MessageKindSpec.For(kind), $"{kind} has no fixed behaviour.");
        }
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_EachKindHasTheDeclaredDefault()
    {
        Assert.AreEqual(MessageDialogButton.Cancel, MessageKindSpec.For(MessageKind.Acknowledge).DefaultButton);
        Assert.AreEqual(MessageDialogButton.Act, MessageKindSpec.For(MessageKind.Confirm).DefaultButton);
        Assert.AreEqual(MessageDialogButton.Act, MessageKindSpec.For(MessageKind.Decide).DefaultButton);

        // The one that matters most: a destructive answer is never the one Enter takes.
        Assert.AreEqual(MessageDialogButton.Cancel, MessageKindSpec.For(MessageKind.DestructiveConfirm).DefaultButton);

        Assert.AreEqual(MessageDialogButton.Cancel, MessageKindSpec.For(MessageKind.LeaveWithoutSaving).DefaultButton);
        Assert.AreEqual(MessageDialogButton.Act, MessageKindSpec.For(MessageKind.Retry).DefaultButton);
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_EachKindAnswersTheDeclaredWay()
    {
        Assert.AreEqual(MessageOutcome.Acknowledged, MessageKindSpec.OutcomeFor(MessageKind.Acknowledge, MessageDialogButton.Act));
        Assert.AreEqual(MessageOutcome.Acknowledged, MessageKindSpec.OutcomeFor(MessageKind.Acknowledge, MessageDialogButton.Cancel));

        foreach (var kind in s_allKinds.Where(k => k != MessageKind.Acknowledge))
        {
            Assert.AreEqual(MessageOutcome.Act, MessageKindSpec.OutcomeFor(kind, MessageDialogButton.Act), $"{kind} lost its affirmative answer.");
            Assert.AreEqual(MessageOutcome.Cancel, MessageKindSpec.OutcomeFor(kind, MessageDialogButton.Cancel), $"{kind} lost its dismissive answer.");
        }
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_AnAcknowledgementOffersOneButton()
    {
        var labels = MessageKindSpec.For(MessageKind.Acknowledge).ResolveLabels(new MessageDialogRequest
        {
            Kind = MessageKind.Acknowledge,
            Title = "Signed out",
            Message = "The application restarted.",
        });

        Assert.IsNull(labels.ActLabel, "an acknowledgement has nothing to accept, so it must not offer a second button");
        Assert.IsFalse(string.IsNullOrWhiteSpace(labels.CancelLabel), "the one button still needs a word on it");
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_TheKindsOwnWordsAreUsedWhenTheScreenSuppliesNone()
    {
        Assert.AreEqual("Yes", MessageKindSpec.For(MessageKind.Decide).ResolveLabels(Request(MessageKind.Decide)).ActLabel);
        Assert.AreEqual("No", MessageKindSpec.For(MessageKind.Decide).ResolveLabels(Request(MessageKind.Decide)).CancelLabel);
        Assert.AreEqual("Leave", MessageKindSpec.For(MessageKind.LeaveWithoutSaving).ResolveLabels(Request(MessageKind.LeaveWithoutSaving)).ActLabel);
        Assert.AreEqual("Stay", MessageKindSpec.For(MessageKind.LeaveWithoutSaving).ResolveLabels(Request(MessageKind.LeaveWithoutSaving)).CancelLabel);
        Assert.AreEqual("Try again", MessageKindSpec.For(MessageKind.Retry).ResolveLabels(Request(MessageKind.Retry)).ActLabel);
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_TheScreensOwnWordsReplaceTheKinds()
    {
        var labels = MessageKindSpec.For(MessageKind.DestructiveConfirm).ResolveLabels(new MessageDialogRequest
        {
            Kind = MessageKind.DestructiveConfirm,
            Title = "Remove this lugger?",
            Message = "Its position is released.",
            ActLabel = "Remove",
            CancelLabel = "Keep",
        });

        Assert.AreEqual("Remove", labels.ActLabel, "a screen's own verb belongs on the affirmative button");
        Assert.AreEqual("Keep", labels.CancelLabel, "a screen's own word belongs on the dismissive button");
    }

    [TestMethod]
    public void Audit_MessageDialogContractTests_TheKindsDefaultIsTheAnswerReportedAsDefault()
    {
        foreach (var kind in s_allKinds)
        {
            var spec = MessageKindSpec.For(kind);

            Assert.IsTrue(MessageKindSpec.IsDefault(kind, spec.DefaultButton), $"{kind} does not report its own default as the default.");
            Assert.IsFalse(
                MessageKindSpec.IsDefault(kind, spec.DefaultButton == MessageDialogButton.Act ? MessageDialogButton.Cancel : MessageDialogButton.Act),
                $"{kind} reports both buttons as the default");
        }
    }

    private static MessageDialogRequest Request(MessageKind kind) => new()
    {
        Kind = kind,
        Title = "A title that names the subject",
        Message = "A message that names what the default answer does.",
    };
}
