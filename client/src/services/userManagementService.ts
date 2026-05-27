import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type { CreateCustomerRequest, CustomerDetails, JsonData, StaffUserGrid, UpdateUserAccessRequest, UserAccess } from "@/types";

export const userManagementService = {
  async getUsers() {
    const response = await api.get<JsonData<UserAccess[]>>("admin/users");
    return unwrapCommonModel(response, "Could not load users");
  },

  async getCustomerGridUsers() {
    const response = await api.get<JsonData<StaffUserGrid[]>>("users");
    return unwrapCommonModel(response, "Could not load customer users");
  },

  async getUserById(userId: number) {
    const response = await api.get<JsonData<UserAccess>>(`users/${userId}`);
    return unwrapCommonModel(response, "Could not load user details");
  },

  async updateUserAccess(userId: number, payload: UpdateUserAccessRequest) {
    const response = await api.put<JsonData<UserAccess>>(`admin/users/${userId}/access`, payload);
    return unwrapCommonModel(response, "Could not update user access");
  },

  async createCustomerForUser(userId: number, payload: CreateCustomerRequest) {
    const response = await api.post<JsonData<CustomerDetails>>(`users/${userId}/customer`, payload);
    return unwrapCommonModel(response, "Could not create customer for user");
  },
};
