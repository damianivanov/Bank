import type { CreditStatus } from "../../Enums/CreditStatus";
import type { CreditType } from "../../Enums/CreditType";

export interface CreditModel
{
	id: number;
	customerId: number;
	customerDisplayName: string;
	creditType: CreditType;
	grantedAmount: number;
	termMonths: number;
	appliedAnnualInterestRate: number;
	appliedGrantingFee: number;
	customerWasVipAtCreation: boolean;
	plannedMonthlyPaymentAmount: number;
	status: CreditStatus;
	grantedAtUtc: string;
	repaidAtUtc?: string;
}
