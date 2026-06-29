import type { JsonModels } from "../../../backend";

export interface AccountOperationResultModel
{
	accountId: number;
	accountIban: string;
	newBalance: number;
	transaction: JsonModels.Bank.MoneyOperations.MoneyTransactionModel;
}
