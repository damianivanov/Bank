namespace Bank.Web.Infrastructure.Authorization;

// RequireStaff/RequireAdmin са йерархични (по-висока роля удовлетворява по-ниско изискване).
// RequireCustomer е порта за точна роля за self-service зоната, ограничена по собственост.
public static class Policies
{
    public const string RequireStaff = nameof(RequireStaff);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireCustomer = nameof(RequireCustomer);
}
