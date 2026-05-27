using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Auth;

public class RegisterCustomerRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PersonalIdentifier { get; set; }

    [StringLength(20)]
    public string? CompanyIdentifier { get; set; }
}
