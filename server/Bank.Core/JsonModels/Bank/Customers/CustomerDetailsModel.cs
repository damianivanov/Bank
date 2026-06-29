using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CustomerDetailsModel
{
    public long Id { get; set; }
    public CustomerType CustomerType { get; set; }
    public bool IsVip { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonalIdentifier { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyIdentifier { get; set; }
    public IReadOnlyCollection<CompanyRepresentativeModel> Representatives { get; set; } = [];
    public IReadOnlyCollection<CustomerAccountSummaryModel> Accounts { get; set; } = [];
    public IReadOnlyCollection<CustomerCreditSummaryModel> Credits { get; set; } = [];
}
