import type { JsonModels } from "../../../backend";
import type { CustomerType } from "../../Enums/CustomerType";

export interface UpdateCustomerRequest
{
	customerType: CustomerType;
	firstName?: string;
	lastName?: string;
	personalIdentifier?: string;
	companyName?: string;
	companyIdentifier?: string;
	representatives?: JsonModels.Bank.Customers.CustomerRepresentativeRequest[];
}
