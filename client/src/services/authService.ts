import type {
  AuthResponse,
  JsonData,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "@/types";
import api from "@/lib/api";

export const authService = {
  login(payload: LoginRequest) {
    return api.post<JsonData<AuthResponse>>("auth/login", payload);
  },

  register(payload: RegisterRequest) {
    return api.post<JsonData<AuthResponse>>("auth/register", payload);
  },

  refresh() {
    return api.post<JsonData<AuthResponse>>("auth/refresh");
  },

  getCurrentUser() {
    return api.get<JsonData<User>>("auth/current-user");
  },

  updateProfile(payload: UpdateProfileRequest) {
    return api.put<JsonData<User>>("auth/profile", payload);
  },

  logout() {
    return api.post<JsonData<string>>("auth/logout", {});
  },
};
