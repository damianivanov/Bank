namespace Bank.Core.JsonModels.Auth;

// Обобщение за административния грид с достъп на потребителите. Броят се изчислява върху всички
// потребители, преди да се приложат търсене и фасетни филтри.
public class UserAccessSummaryModel
{
    public int TotalUsers { get; set; }
    public int Admins { get; set; }
    public int Staff { get; set; }
    public int Customers { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
