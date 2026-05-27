namespace Bank.DB.Constants;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string User = "User";
    public const string Customer = "Customer";
    public const string StaffOrAdmin = Staff + "," + Admin;
}
