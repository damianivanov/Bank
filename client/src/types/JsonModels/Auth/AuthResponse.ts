import type { JsonModels } from "../../backend";

export interface AuthResponse
{
	user: JsonModels.Auth.UserModel;
	tokenExpiresAtUtc: string;
	refreshTokenExpiresAtUtc: string;
}
