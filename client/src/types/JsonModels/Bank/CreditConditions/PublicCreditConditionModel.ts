import type { CreditType } from "../../Enums/CreditType";
import type { PaymentType } from "../../Enums/PaymentType";

export interface PublicCreditConditionModel
{
	id: number;
	creditType: CreditType;
	name: string;
	standardAnnualInterestRate: number;
	maximumAmount: number;
	maximumTermMonths: number;
	defaultPaymentType: PaymentType;
	promoPeriodMonths: number;
	standardPromoRate?: number;
	gracePeriodMonths: number;
	standardGrantingFee: number;
	standardMonthlyManagementFee: number;
	standardAnnualManagementFee: number;
}
