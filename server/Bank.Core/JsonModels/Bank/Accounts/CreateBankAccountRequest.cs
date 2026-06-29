using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Accounts;

public class CreateBankAccountRequest
{
    // [Required] е без ефект върху non-nullable long (0 минава); реален account id трябва да е положителен.
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Range(0, 1000000000)]
    public decimal OpeningBalance { get; set; }
}
