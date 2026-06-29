namespace Bank.Core.JsonModels.Diagnostics;

// Ред от админ грида с логнати API грешки (от таблицата Errors). Само за преглед.
public class ApiErrorModel
{
    public long Id { get; set; }
    public DateTime DateCreated { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Path { get; set; }
    public string? UserName { get; set; }
}
