import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreateCustomerRequest,
  CustomerDetails,
  CustomerLookup,
  Customer as CustomerListItem,
  JsonData,
  UpdateCustomerRequest,
  UpdateCustomerVipRequest,
} from "@/types";

export const customerService = {
  async getCustomers() {
    const response = await api.get<JsonData<CustomerListItem[]>>("customers");
    return unwrapCommonModel(response, "Could not load customers");
  },

  async getCustomerLookup() {
    const response = await api.get<JsonData<CustomerLookup[]>>("customers/lookup");
    return unwrapCommonModel(response, "Could not load customers");
  },

  async getCustomer(customerId: number) {
    const response = await api.get<JsonData<CustomerDetails>>(`customers/${customerId}`);
    return unwrapCommonModel(response, "Could not load customer");
  },

  async createCustomer(payload: CreateCustomerRequest) {
    const response = await api.post<JsonData<CustomerDetails>>("customers", payload);
    return unwrapCommonModel(response, "Could not create customer");
  },

  async updateCustomer(customerId: number, payload: UpdateCustomerRequest) {
    const response = await api.put<JsonData<CustomerDetails>>(`customers/${customerId}`, payload);
    return unwrapCommonModel(response, "Could not update customer");
  },

  async updateCustomerVip(customerId: number, payload: UpdateCustomerVipRequest) {
    const response = await api.put<JsonData<CustomerDetails>>(`customers/${customerId}/vip`, payload);
    return unwrapCommonModel(response, "Could not update VIP status");
  },
};
