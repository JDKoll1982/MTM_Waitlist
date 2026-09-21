using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One person who differs from their role's baseline for the chosen feature, marked granted or denied and marked
/// switched off where they are (FR-076, FR-079).
/// </summary>
/// <remarks>
/// The person is named individually rather than counted, because that is what makes the answer short: a role's
/// baselines cover most people, so the ones worth reading are the exceptions.
/// </remarks>
public sealed class PermissionHolderRow
{
    public PermissionHolderRow(PermissionHolder holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        UserId = holder.UserId;
        DisplayName = holder.DisplayName;
        EmployeeIdentifier = holder.EmployeeIdentifier;
        RoleCode = holder.RoleCode;
        IsSwitchedOff = holder.IsSwitchedOff;
        IsGranted = holder.IsGranted;
    }

    /// <summary>The person's id, which is what opening their own page needs (FR-077).</summary>
    public long UserId { get; }

    public string DisplayName { get; }

    public string EmployeeIdentifier { get; }

    public string RoleCode { get; }

    /// <summary>Whether they hold the feature: they were granted it, or had it taken away from their baseline.</summary>
    public bool IsGranted { get; }

    /// <summary>Whether the person has been switched off, so the view can mark them (FR-079).</summary>
    public bool IsSwitchedOff { get; }

    /// <summary>The role's displayed text, from the resource keyed on the code.</summary>
    public string RoleText => UserDetail.RoleTextFor(RoleCode);

    /// <summary>Whether they were granted it or denied it, in one word.</summary>
    public string MarkText => IsGranted
        ? "Permissions_Holders.Granted".GetLocalized()
        : "Permissions_Holders.Denied".GetLocalized();

    /// <summary>The mark a switched-off holder carries, and an empty string for everybody else.</summary>
    public string SwitchedOffText => IsSwitchedOff ? "Permissions_Holders.SwitchedOff".GetLocalized() : string.Empty;

    /// <summary>What the row announces when it is reached: who they are, their mark, and what opening it does.</summary>
    public string OpenAnnouncement
    {
        get
        {
            var status = IsSwitchedOff ? $", {SwitchedOffText}" : string.Empty;
            return $"{DisplayName}, {RoleText}, {MarkText}{status}. {"Permissions_Holders.People.Heading".GetLocalized()}";
        }
    }
}

/// <summary>
/// The who-holds-this view: the roles whose baselines give a feature, and only the people who differ from their own
/// role's baseline (FR-075, FR-076).
/// </summary>
/// <remarks>
/// <b>Read-only, and nothing here changes anything.</b> Every change still happens on the person's own page
/// (FR-077). The roles come from the seeded baselines the declaration's two-direction check ties to it, read once
/// from the store rather than restated beside the view, so the two cannot drift.
/// </remarks>
public sealed class PermissionHoldersView
{
    public PermissionHoldersView(string featureKey, string featureLabelText, PermissionHolders holders)
    {
        ArgumentNullException.ThrowIfNull(holders);

        FeatureKey = featureKey;
        FeatureLabelText = featureLabelText;
        RoleTexts = holders.RoleCodes.Select(UserDetail.RoleTextFor).ToArray();
        People = holders.People.Select(holder => new PermissionHolderRow(holder)).ToArray();
        NobodyHoldsIt = holders.NobodyHoldsIt;
    }

    /// <summary>The feature the view is answering about.</summary>
    public string FeatureKey { get; }

    /// <summary>What that feature is called, in the reader's words.</summary>
    public string FeatureLabelText { get; }

    /// <summary>The roles whose baselines give it, in catalogue order.</summary>
    public IReadOnlyList<string> RoleTexts { get; }

    /// <summary>Only the people who differ from their role's baseline, each marked.</summary>
    public IReadOnlyList<PermissionHolderRow> People { get; }

    /// <summary>Whether nobody holds it, which the view states in words rather than as an empty region (FR-078).</summary>
    public bool NobodyHoldsIt { get; }

    /// <summary>Whether there is anybody to list.</summary>
    public bool HasPeople => People.Count > 0;

    /// <summary>Whether any role's baseline gives it.</summary>
    public bool HasRoles => RoleTexts.Count > 0;
}
