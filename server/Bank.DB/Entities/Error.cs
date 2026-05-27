using Bank.DB.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class Error : BaseEntity
{
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(8000)]
    public string? Details { get; set; }

    [MaxLength(500)]
    public string? Path { get; set; }

    [MaxLength(256)]
    public string? UserName { get; set; }
}
