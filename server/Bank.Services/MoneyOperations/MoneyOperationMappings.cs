using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.DB.Entities;
using Bank.Services.Common;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Споделено мапване Entity -> DTO за модула с пари, така че customer- и staff-частта да дават еднакви форми.
/// </summary>
internal static class MoneyOperationMappings
{
    public static MoneyTransactionModel MapTransaction(MoneyTransaction transaction)
    {
        return new MoneyTransactionModel
        {
            Id = transaction.Id,
            BankAccountId = transaction.BankAccountId,
            Type = transaction.Type,
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            CreditId = transaction.CreditId,
            CreditPaymentId = transaction.CreditPaymentId,
            DepositRequestId = transaction.DepositRequestId,
            DateCreated = transaction.DateCreated,
        };
    }

    public static AccountOperationResultModel BuildAccountResult(string accountIban, MoneyTransaction transaction)
    {
        return new AccountOperationResultModel
        {
            AccountId = transaction.BankAccountId,
            AccountIban = accountIban,
            NewBalance = transaction.BalanceAfter,
            Transaction = MapTransaction(transaction),
        };
    }

    public static DepositRequestModel MapDepositRequest(DepositRequest depositRequest, string accountIban)
    {
        return new DepositRequestModel
        {
            Id = depositRequest.Id,
            BankAccountId = depositRequest.BankAccountId,
            AccountIban = accountIban,
            Amount = depositRequest.Amount,
            Status = depositRequest.Status,
            ReviewNote = depositRequest.ReviewNote,
            ReviewedAtUtc = depositRequest.ReviewedAtUtc,
            DateCreated = depositRequest.DateCreated,
        };
    }

    /// <summary>Изисква зареден <c>BankAccount.Customer</c> (с Person/Company) за display name.</summary>
    public static DepositRequestQueueModel MapDepositRequestQueue(DepositRequest depositRequest)
    {
        var account = depositRequest.BankAccount;
        return new DepositRequestQueueModel
        {
            Id = depositRequest.Id,
            BankAccountId = depositRequest.BankAccountId,
            AccountIban = account.IBAN,
            CustomerId = account.CustomerId,
            CustomerDisplayName = CustomerDisplayNameFormatter.BuildDisplayName(account.Customer),
            Amount = depositRequest.Amount,
            Status = depositRequest.Status,
            ReviewNote = depositRequest.ReviewNote,
            ReviewedAtUtc = depositRequest.ReviewedAtUtc,
            DateCreated = depositRequest.DateCreated,
        };
    }

    public static CreditPaymentModel MapPayment(CreditInstallment installment)
    {
        return new CreditPaymentModel
        {
            Id = installment.Id,
            PaymentNumber = installment.InstallmentNumber,
            DueDate = installment.DueDate,
            PaymentAmount = installment.InstallmentAmount,
            PrincipalPart = installment.PrincipalPart,
            InterestPart = installment.InterestPart,
            RemainingPrincipalAfterPayment = installment.RemainingPrincipalAfterPayment,
            Status = installment.Status,
            PaidAtUtc = installment.PaidAtUtc,
        };
    }
}
