using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Преглед и решение по заявки за депозит от страна на служител. Одобрението е единственото нещо, което
/// реално кредитира салдо при депозит — затова е под staff authorization, optimistic concurrency и идемпотентност.
/// </summary>
public interface IDepositApprovalService
{
    Task<PagedResponse<DepositRequestQueueModel>> GetDepositRequestsAsync(
        DepositRequestStatus? status,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResultModel> ApproveAsync(long depositRequestId, CancellationToken cancellationToken = default);

    Task<DepositRequestQueueModel> RejectAsync(
        long depositRequestId,
        DepositRejectRequest request,
        CancellationToken cancellationToken = default);
}
