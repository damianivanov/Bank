import type {
  AuthResponse,
  JsonData,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "@/types";
import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";

export const authService = {
  async login(payload: LoginRequest) {
    const response = await api.post<JsonData<AuthResponse>>("auth/login", payload);
    return unwrapCommonModel(response, "Login failed");
  },

  async register(payload: RegisterRequest) {
    const response = await api.post<JsonData<AuthResponse>>("auth/register", payload);
    return unwrapCommonModel(response, "Registration failed");
  },

  async refresh() {
    const response = await api.post<JsonData<AuthResponse>>("auth/refresh");
    return unwrapCommonModel(response, "Refresh failed");
  },

  async getCurrentUser() {
    const response = await api.get<JsonData<User>>("auth/current-user");
    return unwrapCommonModel(response, "Could not load current user");
  },

  async updateProfile(payload: UpdateProfileRequest) {
    const response = await api.put<JsonData<User>>("auth/profile", payload);
    return unwrapCommonModel(response, "Profile could not be updated");
  },

  async logout() {
    const response = await api.post<JsonData<string>>("auth/logout", {});
    return unwrapCommonModel(response, "Could not logout");
  },
};
