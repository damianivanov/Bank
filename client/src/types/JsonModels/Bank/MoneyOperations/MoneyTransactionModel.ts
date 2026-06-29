import type { MoneyTransactionType } from "../../Enums/MoneyTransactionType";

export interface MoneyTransactionModel
{
	id: number;
	bankAccountId: number;
	type: MoneyTransactionType;
	amount: number;
	balanceAfter: number;
	creditId?: number;
	creditPaymentId?: number;
	depositRequestId?: number;
	dateCreated: string;
}
