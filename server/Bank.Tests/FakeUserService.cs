using Bank.Core.JsonModels.Auth;
using Bank.DB.Entities;
using Bank.Services.Users;

namespace Bank.Tests;

internal sealed class FakeUserService : IUserService
{
    public FakeUserService(long? userId = 1, bool isAdmin = false, User? loggedInUser = null)
    {
        LoggedInUserId = userId;
        LoggedInUserIsAdmin = isAdmin;
        LoggedInUser = loggedInUser;
    }

    public long? LoggedInUserId { get; private set; }
    public bool LoggedInUserIsAdmin { get; private set; }
    public User? LoggedInUser { get; private set; }

    public long GetRequiredLoggedInUserId()
    {
        if (!LoggedInUserId.HasValue)
        {
            throw new InvalidOperationException("Test user id is missing.");
        }

        return LoggedInUserId.Value;
    }

    public void InvalidateLoggedInUserCache()
    {
    }

    public Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyCollection<UserAccessModel>> GetUsersForAdministrationAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyCollection<StaffUserGridModel>> GetStaffUsersForManagementAsync(CancellationToken cancellationToken = default)
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

    public Task<UserAccessModel> LinkUserToCustomerAsync(long userId, long customerId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
