import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreateCustomerRequest,
  Customer,
  JsonData,
  PagedRequest,
  RegisterCounterCustomerRequest,
  StaffUserPage,
  UpdateUserAccessRequest,
  UserAccess,
  UserAccessPage,
  UserRole,
} from "@/types";

// Масивите се сериализират като повтарящи се ключове (roles=0&roles=1), за да ги обвърже ASP.NET в UserRole[].
const repeatArrayParams = { indexes: null } as const;

export const userManagementService = {
  async getUsers(request: PagedRequest, roles?: UserRole[], isActive?: boolean) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<UserAccessPage>>("admin/users", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
        roles: roles && roles.length > 0 ? roles : undefined,
        isActive,
      },
      paramsSerializer: { indexes: repeatArrayParams.indexes },
    });
    return unwrapCommonModel(response, "Потребителите не бяха заредени");
  },

  async getAllUsers(request: PagedRequest, linked?: boolean, isActive?: boolean) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<StaffUserPage>>("users", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
        linked,
        isActive,
      },
    });
    return unwrapCommonModel(response, "Потребителите не бяха заредени");
  },

  async getUserById(userId: number) {
    const response = await api.get<JsonData<UserAccess>>(`users/${userId}`);
    return unwrapCommonModel(response, "Данните за потребителя не бяха заредени");
  },

  async updateUserAccess(userId: number, payload: UpdateUserAccessRequest) {
    const response = await api.put<JsonData<UserAccess>>(`admin/users/${userId}/access`, payload);
    return unwrapCommonModel(response, "Достъпът на потребителя не бе обновен");
  },

  async createCustomerForUser(userId: number, payload: CreateCustomerRequest) {
    const response = await api.post<JsonData<Customer>>(`users/${userId}/customer`, payload);
    return unwrapCommonModel(response, "Клиентът за потребителя не бе създаден");
  },

  async createCounterUser(payload: RegisterCounterCustomerRequest) {
    const response = await api.post<JsonData<Customer>>("users", payload);
    return unwrapCommonModel(response, "Потребителят не бе създаден");
  },
};
