import type { CustomerType } from "../../Enums/CustomerType";

export interface CreateCustomerRequest
{
	customerType: CustomerType;
	firstName?: string;
	lastName?: string;
	personalIdentifier?: string;
	companyName?: string;
	companyIdentifier?: string;
	representativeName?: string;
}
