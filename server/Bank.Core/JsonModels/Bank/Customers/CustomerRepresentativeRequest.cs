using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CustomerRepresentativeRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Egn { get; set; } = string.Empty;

    [EnumDataType(typeof(RepresentativeRole))]
    public RepresentativeRole Role { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}
