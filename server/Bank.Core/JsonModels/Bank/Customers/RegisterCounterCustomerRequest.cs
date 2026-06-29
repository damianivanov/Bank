using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Customers;

// Заявка за създаване на клиент на гише: имейл (= username) + един носещ лицето на логина.
// За физическо лице носещият Е клиентът; за юридическо лице носещият е единственият представител,
// а клиентът е фирмата.
public class RegisterCounterCustomerRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [EnumDataType(typeof(CustomerType))]
    public CustomerType CustomerType { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Egn { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CompanyName { get; set; }

    [StringLength(20)]
    public string? CompanyIdentifier { get; set; }

    public RepresentativeRole? RepresentativeRole { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}
