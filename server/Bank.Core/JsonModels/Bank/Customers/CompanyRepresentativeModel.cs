using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CompanyRepresentativeModel
{
    public long PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Egn { get; set; } = string.Empty;
    public RepresentativeRole Role { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
