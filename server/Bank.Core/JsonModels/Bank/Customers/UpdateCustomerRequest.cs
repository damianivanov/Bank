using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Customers;

public class UpdateCustomerRequest
{
    [EnumDataType(typeof(CustomerType))]
    public CustomerType CustomerType { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? PersonalIdentifier { get; set; }

    [StringLength(200)]
    public string? CompanyName { get; set; }

    [StringLength(20)]
    public string? CompanyIdentifier { get; set; }

    public IReadOnlyCollection<CustomerRepresentativeRequest>? Representatives { get; set; }
}
