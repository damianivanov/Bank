import type { RepresentativeRole } from "../../Enums/RepresentativeRole";

export interface CustomerRepresentativeRequest
{
	firstName: string;
	lastName: string;
	egn: string;
	role: RepresentativeRole;
	validFrom?: string;
	validTo?: string;
}
