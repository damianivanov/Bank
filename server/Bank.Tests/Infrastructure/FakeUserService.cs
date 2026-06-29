using Bank.Core.JsonModels.Auth;
using Bank.DB.Entities;
using Bank.Services.Users;

namespace Bank.Tests.Infrastructure;

internal sealed class FakeUserService : IUserService
{
    public FakeUserService(long? userId = 1, bool isAdmin = false, User? loggedInUser = null, long? personId = null)
    {
        LoggedInUserId = userId;
        LoggedInUserIsAdmin = isAdmin;
        LoggedInUser = loggedInUser;
        LoggedInPersonId = personId;
    }

    public long? LoggedInUserId { get; private set; }
    public long? LoggedInPersonId { get; private set; }
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

    public long GetRequiredLoggedInPersonId()
    {
        if (!LoggedInPersonId.HasValue)
        {
            throw new InvalidOperationException("Test person id is missing.");
        }

        return LoggedInPersonId.Value;
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
}
