using Bank.Core.Enums;
using Bank.DB.Entities.Base;

namespace Bank.DB.Entities;

public class Customer : BaseTrackUserEntity
{
    public CustomerType CustomerType { get; set; }
    public bool IsVip { get; set; }

    public long? PersonId { get; set; }
    public Person? Person { get; set; }

    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public ICollection<BankAccount> Accounts { get; set; } = [];
    public ICollection<Credit> Credits { get; set; } = [];
}
