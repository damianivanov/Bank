import type { JsonModels } from "../../backend";
import type { PaymentType } from "../Enums/PaymentType";

export interface CreditCalculatorRequest
{
	loanAmount: number;
	termInMonths: number;
	interestRate: number;
	paymentType: PaymentType;
	promoPeriod?: number;
	promoRate?: number;
	gracePeriod?: number;
	applicationFee?: JsonModels.Calculators.Fee;
	processingFee?: JsonModels.Calculators.Fee;
	otherInitialFees?: JsonModels.Calculators.Fee;
	annualManagementFee?: JsonModels.Calculators.Fee;
	otherAnnualFees?: JsonModels.Calculators.Fee;
	monthlyManagementFee?: JsonModels.Calculators.Fee;
	otherMonthlyFees?: JsonModels.Calculators.Fee;
}
