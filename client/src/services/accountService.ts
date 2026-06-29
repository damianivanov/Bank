import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  BankAccount,
  BankAccountDetails,
  CreateBankAccountRequest,
  JsonData,
  PagedRequest,
  PagedResponse,
} from "@/types";

export const accountService = {
  async getAccounts(request: PagedRequest) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<PagedResponse<BankAccount>>>("accounts", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
      },
    });
    return unwrapCommonModel(response, "Сметките не бяха заредени");
  },

  async getAccount(accountId: number) {
    const response = await api.get<JsonData<BankAccountDetails>>(`accounts/${accountId}`);
    return unwrapCommonModel(response, "Сметката не бе заредена");
  },

  async createAccount(payload: CreateBankAccountRequest) {
    const response = await api.post<JsonData<BankAccountDetails>>("accounts", payload);
    return unwrapCommonModel(response, "Сметката не бе създадена");
  },

  async closeAccount(accountId: number) {
    const response = await api.put<JsonData<BankAccountDetails>>(`accounts/${accountId}/close`);
    return unwrapCommonModel(response, "Сметката не бе закрита");
  },
};
