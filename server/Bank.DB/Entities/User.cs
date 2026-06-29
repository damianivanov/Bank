using Bank.DB.Entities.Base;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class User : IdentityUser<long>, IBaseEntity
{
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Маркер за принудителна смяна на паролата при първо влизане. Вдига се при създаване на акаунт
    // на гише (парола = ЕГН, отгатваема) и пада при първата успешна смяна на паролата.
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }

    public long? PersonId { get; set; }
    public Person? Person { get; set; }
    public ICollection<Token> Tokens { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class Role : IdentityRole<long>
{
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class UserRole : IdentityUserRole<long>
{
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
