import type {
  AuthResponse,
  ChangePasswordRequest,
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
    return unwrapCommonModel(response, "Входът е неуспешен");
  },

  async register(payload: RegisterRequest) {
    const response = await api.post<JsonData<string>>("auth/register", payload);
    return unwrapCommonModel(response, "Регистрацията е неуспешна");
  },

  async changePassword(payload: ChangePasswordRequest) {
    const response = await api.post<JsonData<AuthResponse>>("auth/change-password", payload);
    return unwrapCommonModel(response, "Паролата не бе сменена");
  },

  async refresh() {
    const response = await api.post<JsonData<AuthResponse>>("auth/refresh");
    return unwrapCommonModel(response, "Обновяването на сесията е неуспешно");
  },

  async getCurrentUser() {
    const response = await api.get<JsonData<User>>("auth/current-user");
    return unwrapCommonModel(response, "Текущият потребител не бе зареден");
  },

  async updateProfile(payload: UpdateProfileRequest) {
    const response = await api.put<JsonData<User>>("auth/profile", payload);
    return unwrapCommonModel(response, "Профилът не бе обновен");
  },

  async logout() {
    const response = await api.post<JsonData<string>>("auth/logout", {});
    return unwrapCommonModel(response, "Излизането е неуспешно");
  },
};
