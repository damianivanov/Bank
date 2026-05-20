namespace Bank.DB.Entities.Base;

public interface IBaseTrackUserEntity : IBaseEntity
{
    long? CreatedById { get; set; }
    long? ModifiedById { get; set; }
}
