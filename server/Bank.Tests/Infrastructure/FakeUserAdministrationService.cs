using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using Bank.Services.Users.Administration;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Tests.Infrastructure;

internal sealed class FakeUserAdministrationService : IUserAdministrationService
{
    public Task<UserAccessPageModel> GetUsersForAdministrationAsync(PagedRequest request, IReadOnlyCollection<UserRoleModel> roles, bool? isActive, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<StaffUserPageModel> GetRegularUsersAsync(PagedRequest request, bool? linked, bool? isActive, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<UserAccessModel> GetUserForAdministrationAsync(long userId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<UserAccessModel> UpdateUserAccessAsync(long userId, UpdateUserAccessRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<UserAccessModel> LinkUserToPersonAsync(long userId, long personId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<long> CreateCounterUserAsync(string email, string password, bool mustChangePassword, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
