namespace Bank.DB.Entities.Base;

public abstract class BaseEntity : IBaseEntity
{
    public long Id { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
}
