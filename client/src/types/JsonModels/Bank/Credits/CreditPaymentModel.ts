import type { CreditPaymentStatus } from "../../Enums/CreditPaymentStatus";

export interface CreditPaymentModel
{
	id: number;
	paymentNumber: number;
	dueDate: string;
	paymentAmount: number;
	principalPart: number;
	interestPart: number;
	remainingPrincipalAfterPayment: number;
	status: CreditPaymentStatus;
	paidAtUtc?: string;
}
