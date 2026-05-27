import type { BankAccountStatus } from "../../Enums/BankAccountStatus";

export interface BankAccountModel
{
	id: number;
	iban: string;
	balance: number;
	status: BankAccountStatus;
	customerId: number;
	customerDisplayName: string;
	openedAtUtc: string;
	closedAtUtc?: string;
}
