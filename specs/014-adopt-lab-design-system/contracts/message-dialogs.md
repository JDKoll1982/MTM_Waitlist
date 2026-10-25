# Contract: the shared message dialog set

**Feature**: `014-adopt-lab-design-system`  
**Created**: 2026-10-25

This is the interface every screen codes against after this change. Identifiers are pinned by the spec's Verbatim
Constraints and by FR-004; they are copied here unchanged.

## The one entry point

```csharp
namespace MTM_Waitlist.Module_Shared.Dialogs;

public static class MessageDialogHost
{
    public static Task<MessageDialogOutcome> AskAsync(Window owner, MessageDialogRequest request);
}
```

Rules the implementation must keep:

- `owner` is the window the dialog is centred on and belongs to. It is never the desktop, and it is never omitted.
- The call returns only when the window has closed. There is no fire-and-forget overload, because a caller that ignores
  the answer must still wait for the modal window to close.
- The show call is awaited directly. Deferring its continuation is forbidden by the api rules and breaks the modal
  lifetime.
- The call is UI-thread bound. A caller on a background thread marshals before calling, exactly as the existing
  dialogs require.

## The request

```csharp
public sealed record MessageDialogRequest
{
    public required MessageKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActLabel { get; init; }
    public string? CancelLabel { get; init; }
    public string? Subject { get; init; }
}
```

## The answer

```csharp
public enum MessageOutcome { Acknowledged, Act, Cancel }

public readonly record struct MessageDialogOutcome(MessageOutcome Chosen, bool WasDefault);
```

Mapping the caller reads, fixed by the contract:

| Kind | Act | Cancel | Escape | Window close |
|---|---|---|---|---|
| `Acknowledge` | Acknowledged | n/a | Acknowledged | Acknowledged |
| `Confirm` | Act | Cancel | Cancel | Cancel |
| `Decide` | Act (Yes) | Cancel (No) | Cancel | Cancel |
| `DestructiveConfirm` | Act | Cancel | Cancel | Cancel |
| `LeaveWithoutSaving` | Act (Leave) | Cancel (Stay) | Cancel | Cancel |
| `Retry` | Act | Cancel | Cancel | Cancel |

## The kinds

```csharp
public enum MessageKind
{
    Acknowledge,
    Confirm,
    Decide,
    DestructiveConfirm,
    LeaveWithoutSaving,
    Retry,
}
```

## The sizing resources

Declared in `Styles/Dialogs.xaml`, merged in `App.xaml` in the same change. The five keys are pinned by FR-004 and
copied here unchanged.

| Key | Value |
|---|---|
| `DialogMinWidth` | 480 |
| `DialogPreferredWidth` | 720 |
| `DialogMaxWidth` | 880 |
| `DialogMaxBodyHeight` | 640 |
| `DialogBodyMinHeight` | 120 |

Plus the one that already exists and every page reads:

| Key | Value | Declared in |
|---|---|---|
| `PageContentMaxWidth` | 1080 | `Styles/Thickness.xaml` |

## The host the contract is built on

Pinned by the spec's Verbatim Constraints, copied unchanged: `DevWinUI.WindowedContentDialog`.

`MessageDialogHost` satisfies both the request and the api rule by building a `ContentDialog` for the content and
presenting it through that host with `Owner` set. The host is never used directly by a screen, because a direct use
would carry its own button order.

## What a caller may not do

- Construct a `ContentDialog` for a message or a question. FR-012 forbids it, and the audit fails the build.
- Supply the button order or the default action. The kind decides both.
- Supply a blank title or message. The request validates first.
- Use the platform tooltip property instead of the shared tooltip behaviour (FR-028).
- Declare a dialog width, height or body bound of its own (FR-004).
