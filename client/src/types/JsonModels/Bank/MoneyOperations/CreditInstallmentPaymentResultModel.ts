import type { JsonModels } from "../../../backend";
import type { CreditStatus } from "../../Enums/CreditStatus";

export interface CreditInstallmentPaymentResultModel
{
	creditId: number;
	creditStatus: CreditStatus;
	creditRepaidAtUtc?: string;
	payment: JsonModels.Bank.Credits.CreditPaymentModel;
	accountId: number;
	accountIban: string;
	newBalance: number;
	transaction: JsonModels.Bank.MoneyOperations.MoneyTransactionModel;
}
