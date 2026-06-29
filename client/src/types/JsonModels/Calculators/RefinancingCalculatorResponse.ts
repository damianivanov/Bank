import type { JsonModels } from "../../backend";

export interface RefinancingCalculatorResponse
{
	remainingMonths: number;
	remainingPrincipal: number;
	current: JsonModels.Calculators.LoanSideResult;
	new: JsonModels.Calculators.LoanSideResult;
	savings: number;
	shouldYouSwitch: boolean;
}
