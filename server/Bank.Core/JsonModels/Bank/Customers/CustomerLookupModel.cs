using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CustomerLookupModel
{
    public long Id { get; set; }
    public CustomerType CustomerType { get; set; }
    public bool IsVip { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
