using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using Bank.Services.Users.Administration;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Tests.Infrastructure;

/// <summary>
/// Двойник, който само запомня с кое лице е поискано да се свърже даден акаунт. Позволява да се
/// провери коя person_id резолвва <c>CreateCustomerForUserAsync</c>, без да се вдига цял Identity стек.
/// </summary>
internal sealed class RecordingUserAdministrationService : IUserAdministrationService
{
    public long? LastUserId { get; private set; }
    public long? LastPersonId { get; private set; }

    public Task<UserAccessModel> LinkUserToPersonAsync(long userId, long personId, CancellationToken cancellationToken = default)
    {
        LastUserId = userId;
        LastPersonId = personId;
        return Task.FromResult(new UserAccessModel { Id = userId, PersonId = personId });
    }

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

    public Task<long> CreateCounterUserAsync(string email, string password, bool mustChangePassword, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
