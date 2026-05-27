using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CreateCustomerRequest
{
    [Required]
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

    [StringLength(200)]
    public string? RepresentativeName { get; set; }
}
