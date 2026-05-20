using Bank.DB.Entities.Base;

namespace Bank.DB.Entities;

public class Error : BaseEntity
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Path { get; set; }
    public string? UserName { get; set; }
}
