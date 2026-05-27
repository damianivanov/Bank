using Bank.Core.JsonModels.Auth;
using Bank.DB.Entities;

namespace Bank.Services.Users;

public interface IUserService
{
    long? LoggedInUserId { get; }
    bool LoggedInUserIsAdmin { get; }
    User? LoggedInUser { get; }
    long GetRequiredLoggedInUserId();

    Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default);
    Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserAccessModel>> GetUsersForAdministrationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StaffUserGridModel>> GetStaffUsersForManagementAsync(CancellationToken cancellationToken = default);
    Task<UserAccessModel> GetUserForAdministrationAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserAccessModel> UpdateUserAccessAsync(long userId, UpdateUserAccessRequest request, CancellationToken cancellationToken = default);
    Task<UserAccessModel> LinkUserToCustomerAsync(long userId, long customerId, CancellationToken cancellationToken = default);
    void InvalidateLoggedInUserCache();
}
