import type { CalculatorType } from "../Enums/CalculatorType";

export interface SavedCalculationModel
{
	id: number;
	type: CalculatorType;
	name: string;
	createdAtUtc: string;
}
