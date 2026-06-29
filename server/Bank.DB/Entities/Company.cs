using Bank.DB.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class Company : BaseTrackUserEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Eik { get; set; } = string.Empty;

    public ICollection<Customer> Customers { get; set; } = [];

    public ICollection<CompanyRepresentative> Representatives { get; set; } = [];
}
