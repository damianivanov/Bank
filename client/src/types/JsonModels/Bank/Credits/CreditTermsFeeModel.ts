import type { CreditFeeKind } from "../../Enums/CreditFeeKind";
import type { FeeType } from "../../Enums/FeeType";

export interface CreditTermsFeeModel
{
	kind: CreditFeeKind;
	type: FeeType;
	value: number;
}
