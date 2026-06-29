import type { RepresentativeRole } from "../../Enums/RepresentativeRole";

export interface CompanyRepresentativeModel
{
	personId: number;
	firstName: string;
	lastName: string;
	egn: string;
	role: RepresentativeRole;
	validFrom?: string;
	validTo?: string;
}
