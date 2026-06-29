using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using UserRole = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Services.Users.Administration;

public interface IUserAdministrationService
{
    Task<UserAccessPageModel> GetUsersForAdministrationAsync(PagedRequest request, IReadOnlyCollection<UserRole> roles, bool? isActive, CancellationToken cancellationToken = default);
    Task<StaffUserPageModel> GetRegularUsersAsync(PagedRequest request, bool? linked, bool? isActive, CancellationToken cancellationToken = default);
    Task<UserAccessModel> GetUserForAdministrationAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserAccessModel> UpdateUserAccessAsync(long userId, UpdateUserAccessRequest request, CancellationToken cancellationToken = default);
    Task<long> CreateCounterUserAsync(string email, string password, bool mustChangePassword, CancellationToken cancellationToken = default);
    Task<UserAccessModel> LinkUserToPersonAsync(long userId, long personId, CancellationToken cancellationToken = default);
}
