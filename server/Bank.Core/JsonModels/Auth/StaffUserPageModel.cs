namespace Bank.Core.JsonModels.Auth;

// Страница от грида „Всички потребители“ заедно с обобщението върху целия базов набор.
public class StaffUserPageModel
{
    public IReadOnlyCollection<StaffUserGridModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public StaffUserSummaryModel Summary { get; set; } = new();
}
