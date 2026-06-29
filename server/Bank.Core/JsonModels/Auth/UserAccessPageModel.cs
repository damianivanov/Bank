namespace Bank.Core.JsonModels.Auth;

// Страница от административния грид с достъп на потребителите заедно с обобщението върху всички потребители.
public class UserAccessPageModel
{
    public IReadOnlyCollection<UserAccessModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public UserAccessSummaryModel Summary { get; set; } = new();
}
