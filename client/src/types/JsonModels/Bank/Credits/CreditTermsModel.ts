import type { JsonModels } from "../../../backend";
import type { PaymentType } from "../../Enums/PaymentType";

export interface CreditTermsModel
{
	paymentType: PaymentType;
	baseAnnualInterestRate: number;
	promoPeriodMonths: number;
	promoAnnualInterestRate?: number;
	gracePeriodMonths: number;
	apr: number;
	wasVipApplied: boolean;
	plannedMonthlyPaymentAmount: number;
	fees: JsonModels.Bank.Credits.CreditTermsFeeModel[];
}
