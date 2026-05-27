using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreateCreditRequest
{
    [Required]
    public long CustomerId { get; set; }

    [Required]
    public CreditType CreditType { get; set; }

    [Range(0.01, 1000000000)]
    public decimal GrantedAmount { get; set; }

    [Range(1, 1200)]
    public int TermMonths { get; set; }
}
