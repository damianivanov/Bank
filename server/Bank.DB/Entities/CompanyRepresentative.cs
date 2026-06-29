using Bank.Core.Enums;
using Bank.DB.Entities.Base;

namespace Bank.DB.Entities;

public class CompanyRepresentative : BaseTrackUserEntity
{
    public long PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public RepresentativeRole Role { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
