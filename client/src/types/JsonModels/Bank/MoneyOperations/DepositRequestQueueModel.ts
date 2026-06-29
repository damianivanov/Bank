import type { DepositRequestStatus } from "../../Enums/DepositRequestStatus";

export interface DepositRequestQueueModel
{
	id: number;
	bankAccountId: number;
	accountIban: string;
	customerId: number;
	customerDisplayName: string;
	amount: number;
	status: DepositRequestStatus;
	reviewNote?: string;
	reviewedAtUtc?: string;
	dateCreated: string;
}
