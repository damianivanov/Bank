using Bank.Core.JsonModels.Auth;

namespace Bank.Core.JsonModels.Auth;

public class UserModel
{
    public long Id { get; set; }
    public long? PersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool MustChangePassword { get; set; }
    public IReadOnlyCollection<UserRole> Roles { get; set; } = [];
}
