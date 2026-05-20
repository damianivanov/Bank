namespace Bank.DB.Repositories.User;

public interface IUserRepository
{
    Task<Bank.DB.Entities.User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Bank.DB.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
