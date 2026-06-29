using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

// Лек модел за формата за редакция: идентичност + представители, без сметки и кредити.
public class CustomerEditModel
{
    public long Id { get; set; }
    public CustomerType CustomerType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonalIdentifier { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyIdentifier { get; set; }
    public IReadOnlyCollection<CompanyRepresentativeModel> Representatives { get; set; } = [];
}
