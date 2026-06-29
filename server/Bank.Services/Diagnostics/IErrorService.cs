using Bank.Core.JsonModels.Common;
using Bank.Core.JsonModels.Diagnostics;

namespace Bank.Services.Diagnostics;

public interface IErrorService
{
    Task<PagedResponse<ApiErrorModel>> GetErrorsAsync(
        PagedRequest request,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}
