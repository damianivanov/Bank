using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class Customer : BaseTrackUserEntity
{
    public CustomerType CustomerType { get; set; }
    public bool IsVip { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? PersonalIdentifier { get; set; }
    

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(20)]
    public string? CompanyIdentifier { get; set; }

    [MaxLength(200)]
    public string? RepresentativeName { get; set; }

    public User? User { get; set; }
    public ICollection<BankAccount> Accounts { get; set; } = [];
    public ICollection<Credit> Credits { get; set; } = [];
}
