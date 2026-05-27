import type { CustomerType } from "../../Enums/CustomerType";

export interface CustomerModel
{
	id: number;
	customerType: CustomerType;
	isVip: boolean;
	displayName: string;
	identifier: string;
}
