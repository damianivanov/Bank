using Bank.Core.Enums;
using Bank.DB.Entities;

namespace Bank.Services.Common;

public static class CustomerDisplayNameFormatter
{
    public static string BuildDisplayName(Customer customer)
    {
        return customer.CustomerType == CustomerType.Individual
            ? $"{customer.FirstName} {customer.LastName}".Trim()
            : customer.CompanyName ?? string.Empty;
    }

    public static string BuildIdentifier(Customer customer)
    {
        return customer.CustomerType == CustomerType.Individual
            ? customer.PersonalIdentifier ?? string.Empty
            : customer.CompanyIdentifier ?? string.Empty;
    }
}
