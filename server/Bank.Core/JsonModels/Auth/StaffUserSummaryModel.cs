namespace Bank.Core.JsonModels.Auth;

// Обобщение за грида „Всички потребители“ (служебния изглед). Броят се изчислява върху базовия набор от
// обикновени потребители (без Admin/Staff), преди да се приложат търсене и филтри.
public class StaffUserSummaryModel
{
    public int Total { get; set; }
    public int Linked { get; set; }
    public int MissingCustomer { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
