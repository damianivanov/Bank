using Bank.Core.JsonModels.Common;
using Bank.Core.JsonModels.Diagnostics;
using Bank.DB;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Diagnostics;

public class ErrorService : IErrorService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;

    public ErrorService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PagedResponse<ApiErrorModel>> GetErrorsAsync(
        PagedRequest request,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var filtered = dbContext.Errors.AsNoTracking().AsQueryable();

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(e =>
                e.Message.ToLower().Contains(search)
                || (e.Path != null && e.Path.ToLower().Contains(search))
                || (e.UserName != null && e.UserName.ToLower().Contains(search)));
        }

        // Филтър по период (включителен, с дневна точност). DateCreated се пази в UTC.
        if (fromDate.HasValue)
        {
            var lowerBound = fromDate.Value.Date;
            filtered = filtered.Where(e => e.DateCreated >= lowerBound);
        }

        if (toDate.HasValue)
        {
            var upperBound = toDate.Value.Date.AddDays(1);
            filtered = filtered.Where(e => e.DateCreated < upperBound);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния диапазон, за да не препълни int32 изчислението на Skip.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var errors = await filtered
            .OrderByDescending(e => e.DateCreated)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ApiErrorModel
            {
                Id = e.Id,
                DateCreated = e.DateCreated,
                Message = e.Message,
                Details = e.Details,
                Path = e.Path,
                UserName = e.UserName,
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ApiErrorModel>
        {
            Items = errors,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
