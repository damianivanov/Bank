import type { UserRole } from "../Enums/UserRole";

export interface UserModel
{
	id: number;
	personId?: number;
	email: string;
	firstName?: string;
	lastName?: string;
	mustChangePassword: boolean;
	roles: UserRole[];
}
