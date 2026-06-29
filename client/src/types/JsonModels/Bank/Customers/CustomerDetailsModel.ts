import type { JsonModels } from "../../../backend";
import type { CustomerType } from "../../Enums/CustomerType";

export interface CustomerDetailsModel
{
	id: number;
	customerType: CustomerType;
	isVip: boolean;
	firstName?: string;
	lastName?: string;
	personalIdentifier?: string;
	companyName?: string;
	companyIdentifier?: string;
	representatives: JsonModels.Bank.Customers.CompanyRepresentativeModel[];
	accounts: JsonModels.Bank.Customers.CustomerAccountSummaryModel[];
	credits: JsonModels.Bank.Customers.CustomerCreditSummaryModel[];
}
