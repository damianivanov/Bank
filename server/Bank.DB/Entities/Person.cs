using Bank.DB.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class Person : BaseTrackUserEntity
{
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Egn { get; set; } = string.Empty;

    public User? User { get; set; }

    public ICollection<Customer> Customers { get; set; } = [];

    public ICollection<CompanyRepresentative> Representations { get; set; } = [];
}
