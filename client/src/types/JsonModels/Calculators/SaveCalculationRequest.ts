import type { JsonModels } from "../../backend";
import type { CalculatorType } from "../Enums/CalculatorType";

export interface SaveCalculationRequest
{
	type: CalculatorType;
	name: string;
	credit?: JsonModels.Calculators.CreditCalculatorRequest;
	leasing?: JsonModels.Calculators.LeasingCalculatorRequest;
	refinancing?: JsonModels.Calculators.RefinancingCalculatorRequest;
}
