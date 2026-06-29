import type { FeeType } from "../Enums/FeeType";

export interface Fee
{
	type: FeeType;
	value: number;
}
