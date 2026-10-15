namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Which of the three themes a waitlist card's status column takes (FR-019, <c>data-model.md</c> §3).
/// </summary>
/// <remarks>
/// Three themes and not eight: the word tells the viewer which state the request is in, and the theme tells them,
/// without their having to read it, whether the row is ordinary, has a condition reported against it, or is past
/// its time. The two reds are deliberately apart so an overdue row and a condition-carrying row are never mistaken
/// for one another (FR-072).
/// </remarks>
public enum StatusColumnTheme
{
    /// <summary>The word is a lifecycle word and the request is not overdue.</summary>
    Normal,

    /// <summary>A handler-set condition is shown, and the column takes a lighter red than overdue (FR-072).</summary>
    HandlerCondition,

    /// <summary>The overdue marking is shown, and the row carries no handler condition (FR-014, FR-023).</summary>
    Overdue,
}
