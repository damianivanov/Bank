namespace Bank.Core.JsonModels.Auth;

public class UserAccessModel
{
    public long Id { get; set; }
    public long? PersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonDisplayName { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyCollection<UserRole> Roles { get; set; } = [];
}
