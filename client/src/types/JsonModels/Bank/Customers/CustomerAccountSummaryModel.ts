import type { BankAccountStatus } from "../../Enums/BankAccountStatus";

export interface CustomerAccountSummaryModel
{
	id: number;
	iban: string;
	balance: number;
	status: BankAccountStatus;
	openedAtUtc: string;
	closedAtUtc?: string;
}
