import type { JsonModels } from "../../../backend";
import type { CreditTermsOrigin } from "../../Enums/CreditTermsOrigin";
import type { PaymentType } from "../../Enums/PaymentType";

export interface CreditTermsHistoryModel
{
	origin: CreditTermsOrigin;
	isCurrent: boolean;
	effectiveFromPaymentNumber: number;
	dateCreated: string;
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
