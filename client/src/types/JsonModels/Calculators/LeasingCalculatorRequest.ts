import type { JsonModels } from "../../backend";

export interface LeasingCalculatorRequest
{
	priceWithVAT: number;
	downPayment: number;
	leasingTerm: number;
	monthlyPayment: number;
	processingFee?: JsonModels.Calculators.Fee;
}
