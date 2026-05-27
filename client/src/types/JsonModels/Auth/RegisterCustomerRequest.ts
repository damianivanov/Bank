export interface RegisterCustomerRequest
{
	email: string;
	password: string;
	personalIdentifier?: string;
	companyIdentifier?: string;
}
