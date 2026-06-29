using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Операции с пари, които клиентът извършва върху СВОИ сметки. Всеки метод се ограничава по собственост чрез
/// <c>accessibleCustomerIds</c> (резолвнати в контролера от person_id), за да не може клиент да докосне чужда
/// сметка/кредит. Депозитът само създава заявка за одобрение; тегленето и плащането на вноска променят салдото
/// веднага под защита на optimistic concurrency + идемпотентност.
/// </summary>
public interface IMoneyOperationService
{
    Task<DepositRequestModel> RequestDepositAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        DepositRequestCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResultModel> WithdrawAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        WithdrawalCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditInstallmentPaymentResultModel> PayCreditInstallmentAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long creditId,
        PayCreditInstallmentRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<MoneyTransactionModel>> GetAccountTransactionsAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DepositRequestModel>> GetMyDepositRequestsAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        CancellationToken cancellationToken = default);
}
