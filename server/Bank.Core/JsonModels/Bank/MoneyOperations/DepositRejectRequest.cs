using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

public class DepositRejectRequest
{
    [StringLength(500)]
    public string? Note { get; set; }
}
