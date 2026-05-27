using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Customers;

public class UpdateCustomerVipRequest
{
    [Required]
    public bool IsVip { get; set; }
}
