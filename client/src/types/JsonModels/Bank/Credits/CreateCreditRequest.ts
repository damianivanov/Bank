import type { JsonModels } from "../../../backend";
import type { CreditType } from "../../Enums/CreditType";
import type { PaymentType } from "../../Enums/PaymentType";

export interface CreateCreditRequest
{
	customerId: number;
	creditType: CreditType;
	grantedAmount: number;
	termMonths: number;
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
