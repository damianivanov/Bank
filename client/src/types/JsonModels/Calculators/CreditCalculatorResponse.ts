import type { JsonModels } from "../../backend";

export interface CreditCalculatorResponse
{
	apr: number;
	averageMonthlyPayment: number;
	totalAmountWithFees: number;
	totalFees: number;
	totalInterest: number;
	totalPayments: number;
	paymentSchedule: JsonModels.Calculators.PaymentScheduleItem[];
}
