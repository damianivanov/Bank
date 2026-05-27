import type { CreditType } from "../../Enums/CreditType";

export interface CreateCreditRequest
{
	customerId: number;
	creditType: CreditType;
	grantedAmount: number;
	termMonths: number;
}
