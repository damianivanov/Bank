import type { UserRole } from "../Enums/UserRole";

export interface UserAccessModel
{
	id: number;
	personId?: number;
	email: string;
	firstName?: string;
	lastName?: string;
	personDisplayName?: string;
	isActive: boolean;
	roles: UserRole[];
}
