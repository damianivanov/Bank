using Bank.Core.JsonModels.Auth;
using Bank.DB.Entities;

namespace Bank.Services.Users;

public interface IUserService
{
    long? LoggedInUserId { get; }
    long? LoggedInPersonId { get; }
    bool LoggedInUserIsAdmin { get; }
    User? LoggedInUser { get; }
    long GetRequiredLoggedInUserId();
    long GetRequiredLoggedInPersonId();

    Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default);
    Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    void InvalidateLoggedInUserCache();
}
