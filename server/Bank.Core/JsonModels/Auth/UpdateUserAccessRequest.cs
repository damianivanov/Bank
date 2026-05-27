namespace Bank.Core.JsonModels.Auth;

public class UpdateUserAccessRequest
{
    public bool IsActive { get; set; }
    public bool IsStaff { get; set; }
    public bool IsAdmin { get; set; }
}
