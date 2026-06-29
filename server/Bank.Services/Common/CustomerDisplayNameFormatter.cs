using Bank.Core.Enums;
using Bank.DB.Entities;

namespace Bank.Services.Common;

public static class CustomerDisplayNameFormatter
{
    public static string BuildDisplayName(Customer customer)
    {
        return customer.CustomerType == CustomerType.Individual
            ? BuildPersonName(customer.Person)
            : customer.Company?.Name ?? string.Empty;
    }

    public static string BuildIdentifier(Customer customer)
    {
        return customer.CustomerType == CustomerType.Individual
            ? customer.Person?.Egn ?? string.Empty
            : customer.Company?.Eik ?? string.Empty;
    }

    public static string BuildPersonName(Person? person)
    {
        return person == null
            ? string.Empty
            : $"{person.FirstName} {person.LastName}".Trim();
    }
}
