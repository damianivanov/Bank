import type { JsonModels } from "../../backend";

export interface RefinancingCalculatorRequest
{
	currentLoan: JsonModels.Calculators.CurrentLoanInput;
	newLoan: JsonModels.Calculators.NewLoanInput;
}
