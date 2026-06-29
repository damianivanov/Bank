import type { DepositRequestStatus } from "../../Enums/DepositRequestStatus";

export interface DepositRequestModel
{
	id: number;
	bankAccountId: number;
	accountIban: string;
	amount: number;
	status: DepositRequestStatus;
	reviewNote?: string;
	reviewedAtUtc?: string;
	dateCreated: string;
}
