namespace Bank.Core.JsonModels.Bank.Customers;

public class UpdateCustomerVipRequest
{
    // bool винаги присъства и двете стойности са валидни, така че няма какво да се валидира на ниво 2.
    public bool IsVip { get; set; }
}
