namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Answers whether a person holds a named permission, resolving their own stored choice, then their role's
/// baseline, then the shipped fallback (FR-049, FR-050, FR-051, FR-053, FR-060).
/// </summary>
/// <remarks>
/// <para>
/// One lookup answers both a control's visibility and the action it guards (FR-056), so a screen never shows a
/// control whose use would then be refused, and a gate refused at the action is refused in the same words as the
/// control's absence (FR-117).
/// </para>
/// <para>
/// The answer is cached for the session and explicitly invalidated when a permission is saved or when a sign-in
/// resolves a different person. It is never read inside a layout pass on the interface thread: every member here
/// is asynchronous, and a caller that needs several answers asks for them in one call so the store is read once.
/// </para>
/// </remarks>
public interface IPermissionService
{
    /// <summary>
    /// Whether the signed-in person holds <paramref name="permissionKey"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="permissionKey"/> is not held by the declaration. A gate that reads an undeclared key is a
    /// failure rather than a silent refusal (FR-061).
    /// </exception>
    Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the signed-in person holds each of <paramref name="permissionKeys"/>, in one store read, so a
    /// screen that needs six answers asks once.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Any requested key is not held by the declaration (FR-061).
    /// </exception>
    Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached answers, so the next lookup reads the store again. Called after a permission is saved,
    /// and when a sign-in resolves a different person.
    /// </summary>
    void Invalidate();
}
