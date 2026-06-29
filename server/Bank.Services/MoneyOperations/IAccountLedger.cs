using Bank.DB.Entities;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Единствената точка, през която се променя салдо по сметка. Записва неизменимо движение в регистъра и
/// синхронно коригира денормализираното <see cref="BankAccount.Balance"/>. НЕ прави SaveChanges — извикващият
/// контролира транзакцията и optimistic-concurrency повторните опити. НЕ проверява достатъчност на салдото
/// (това зависи от вида движение и е отговорност на извикващия).
/// </summary>
public interface IAccountLedger
{
    MoneyTransaction Record(LedgerEntry entry);
}
