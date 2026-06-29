import type { CustomerType } from "../../Enums/CustomerType";
import type { RepresentativeRole } from "../../Enums/RepresentativeRole";

export interface RegisterCounterCustomerRequest
{
	email: string;
	customerType: CustomerType;
	firstName: string;
	lastName: string;
	egn: string;
	companyName?: string;
	companyIdentifier?: string;
	representativeRole?: RepresentativeRole;
	validFrom?: string;
	validTo?: string;
}
