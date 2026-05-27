import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  BankAccount,
  BankAccountDetails,
  CreateBankAccountRequest,
  JsonData,
} from "@/types";

export const accountService = {
  async getAccounts() {
    const response = await api.get<JsonData<BankAccount[]>>("accounts");
    return unwrapCommonModel(response, "Could not load accounts");
  },

  async getAccount(accountId: number) {
    const response = await api.get<JsonData<BankAccountDetails>>(`accounts/${accountId}`);
    return unwrapCommonModel(response, "Could not load account");
  },

  async createAccount(payload: CreateBankAccountRequest) {
    const response = await api.post<JsonData<BankAccountDetails>>("accounts", payload);
    return unwrapCommonModel(response, "Could not create account");
  },

  async closeAccount(accountId: number) {
    const response = await api.put<JsonData<BankAccountDetails>>(`accounts/${accountId}/close`);
    return unwrapCommonModel(response, "Could not close account");
  },
};
