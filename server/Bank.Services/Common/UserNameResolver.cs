using Bank.DB.Entities;

namespace Bank.Services.Common;

// Името на потребител има един източник на истина: ако акаунтът е свързан с лице, то идва от лицето
// (KYC данните), иначе — от самия акаунт (служители и още несвързани регистрации). Очаква Person
// навигацията да е заредена, когато PersonId е попълнен (виж UserService за асинхронното зареждане).
public static class UserNameResolver
{
    public static (string? FirstName, string? LastName) Resolve(User user)
    {
        return user.Person != null
            ? (user.Person.FirstName, user.Person.LastName)
            : (user.FirstName, user.LastName);
    }
}
