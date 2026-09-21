namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The application identity an API caller resolved to: the account, plus the role it holds (T038).
/// </summary>
/// <remarks>
/// The person id travels with the role because the permission a caller holds is stored per person and per role,
/// and the store's own read (<c>sp_config_permissions_user_get</c>) resolves the role from the person's
/// assignment. Carrying the code and name too keeps the refusal log and the authentication ticket readable.
/// </remarks>
/// <param name="UserId">The person's id in the application store.</param>
/// <param name="RoleCode">The role code the person's assignment names, for example <c>developer</c>.</param>
/// <param name="RoleName">That role's display name, for example <c>Developer</c>.</param>
public sealed record ServiceOperatorIdentity(long UserId, string RoleCode, string RoleName);
