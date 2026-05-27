import type { UserRole } from "../Enums/UserRole";

export interface UserAccessModel
{
	id: number;
	customerId?: number;
	email: string;
	firstName?: string;
	lastName?: string;
	customerDisplayName?: string;
	isActive: boolean;
	roles: UserRole[];
}
