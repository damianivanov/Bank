import type { CreditType } from "../../Enums/CreditType";
import type { PaymentType } from "../../Enums/PaymentType";

export interface CreditTypeConditionModel
{
	id: number;
	creditType: CreditType;
	name: string;
	standardAnnualInterestRate: number;
	vipAnnualInterestRate: number;
	maximumAmount: number;
	maximumTermMonths: number;
	standardGrantingFee: number;
	vipGrantingFee: number;
	defaultPaymentType: PaymentType;
	promoPeriodMonths: number;
	standardPromoRate?: number;
	vipPromoRate?: number;
	gracePeriodMonths: number;
	standardMonthlyManagementFee: number;
	vipMonthlyManagementFee: number;
	standardAnnualManagementFee: number;
	vipAnnualManagementFee: number;
	isActive: boolean;
}
