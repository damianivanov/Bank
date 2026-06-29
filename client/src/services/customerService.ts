import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreateCustomerRequest,
  CustomerDetails,
  CustomerEdit,
  CustomerLookup,
  Customer as CustomerListItem,
  CustomerType,
  JsonData,
  PagedRequest,
  PagedResponse,
  UpdateCustomerRequest,
  UpdateCustomerVipRequest,
} from "@/types";

export const customerService = {
  async getCustomers(request: PagedRequest, customerType?: CustomerType) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<PagedResponse<CustomerListItem>>>("customers", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
        customerType,
      },
    });
    return unwrapCommonModel(response, "Клиентите не бяха заредени");
  },

  async getCustomerLookup(search?: string) {
    const trimmed = search?.trim();
    const response = await api.get<JsonData<CustomerLookup[]>>("customers/lookup", {
      params: { search: trimmed ? trimmed : undefined },
    });
    return unwrapCommonModel(response, "Клиентите не бяха заредени");
  },

  async getCustomer(customerId: number) {
    const response = await api.get<JsonData<CustomerDetails>>(`customers/${customerId}`);
    return unwrapCommonModel(response, "Клиентът не бе зареден");
  },

  async getCustomerForEdit(customerId: number) {
    const response = await api.get<JsonData<CustomerEdit>>(`customers/${customerId}/edit`);
    return unwrapCommonModel(response, "Клиентът не бе зареден");
  },

  async createCustomer(payload: CreateCustomerRequest) {
    const response = await api.post<JsonData<CustomerListItem>>("customers", payload);
    return unwrapCommonModel(response, "Клиентът не бе създаден");
  },

  async updateCustomer(customerId: number, payload: UpdateCustomerRequest) {
    const response = await api.put<JsonData<CustomerListItem>>(`customers/${customerId}`, payload);
    return unwrapCommonModel(response, "Клиентът не бе обновен");
  },

  async updateCustomerVip(customerId: number, payload: UpdateCustomerVipRequest) {
    const response = await api.put<JsonData<CustomerDetails>>(`customers/${customerId}/vip`, payload);
    return unwrapCommonModel(response, "VIP статусът не бе обновен");
  },
};
