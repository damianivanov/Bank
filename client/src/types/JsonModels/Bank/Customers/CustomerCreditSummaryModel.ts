import type { CreditStatus } from "../../Enums/CreditStatus";
import type { CreditType } from "../../Enums/CreditType";

export interface CustomerCreditSummaryModel
{
	id: number;
	creditType: CreditType;
	grantedAmount: number;
	termMonths: number;
	appliedAnnualInterestRate: number;
	plannedMonthlyPaymentAmount: number;
	status: CreditStatus;
	grantedAtUtc: string;
	repaidAtUtc?: string;
}
