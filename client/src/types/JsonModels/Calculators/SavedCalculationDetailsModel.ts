import type { JsonModels } from "../../backend";
import type { CalculatorType } from "../Enums/CalculatorType";

export interface SavedCalculationDetailsModel
{
	id: number;
	type: CalculatorType;
	name: string;
	createdAtUtc: string;
	creditInputs?: JsonModels.Calculators.CreditCalculatorRequest;
	creditResult?: JsonModels.Calculators.CreditCalculatorResponse;
	leasingInputs?: JsonModels.Calculators.LeasingCalculatorRequest;
	leasingResult?: JsonModels.Calculators.LeasingCalculatorResponse;
	refinancingInputs?: JsonModels.Calculators.RefinancingCalculatorRequest;
	refinancingResult?: JsonModels.Calculators.RefinancingCalculatorResponse;
}
