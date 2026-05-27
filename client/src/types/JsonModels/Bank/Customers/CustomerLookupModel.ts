import type { CustomerType } from "../../Enums/CustomerType";

export interface CustomerLookupModel
{
	id: number;
	customerType: CustomerType;
	isVip: boolean;
	displayName: string;
}
