namespace Bank.DB.Entities.Base;

public abstract class BaseTrackUserEntity : BaseEntity, IBaseTrackUserEntity
{
    public long? CreatedById { get; set; }
    public long? ModifiedById { get; set; }
}
