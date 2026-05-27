import type { PricingChangeReason } from "../../Enums/PricingChangeReason";

export interface CreditPricingChangeModel
{
	id: number;
	previousAnnualInterestRate: number;
	newAnnualInterestRate: number;
	effectiveFromPaymentNumber: number;
	reason: PricingChangeReason;
	dateCreated: string;
}
