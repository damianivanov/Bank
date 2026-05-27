using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Accounts;

public class CreateBankAccountRequest
{
    [Required]
    public long CustomerId { get; set; }

    [Range(0, 1000000000)]
    public decimal OpeningBalance { get; set; }
}
