import type { CreditType } from "../../Enums/CreditType";

export interface CreditTypeConditionModel
{
	id: number;
	creditType: CreditType;
	name: string;
	standardAnnualInterestRate: number;
	vipAnnualInterestRate: number;
	maximumAmount: number;
	maximumTermMonths: number;
	standardGrantingFee: number;
	vipGrantingFee: number;
	isActive: boolean;
}
