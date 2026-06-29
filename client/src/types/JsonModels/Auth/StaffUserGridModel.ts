import type { UserRole } from "../Enums/UserRole";

export interface StaffUserGridModel
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
