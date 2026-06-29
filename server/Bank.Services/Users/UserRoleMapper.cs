using Bank.DB.Constants;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Services.Users;

// Споделен mapper на ролите между UserService (профил) и UserAdministrationService (гридове),
// за да няма дублиране на логиката за разпознаване на ролевите имена.
internal static class UserRoleMapper
{
    public static IReadOnlyCollection<UserRoleModel> MapRoles(IEnumerable<string?> roles)
    {
        return roles
            .Where(role => role != null)
            .Select(role => MapRole(role!))
            .Distinct()
            .OrderBy(role => (int)role)
            .ToArray();
    }

    public static UserRoleModel MapRole(string role)
    {
        if (string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Admin;
        }

        if (string.Equals(role, RoleNames.Staff, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Staff;
        }

        if (string.Equals(role, RoleNames.Customer, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Customer;
        }

        return UserRoleModel.User;
    }
}
