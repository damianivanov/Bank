import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  AccountOperationResult,
  CreditDetails,
  CreditInstallmentPaymentResult,
  CustomerDetails,
  CustomerLookup,
  DepositRequest,
  DepositRequestCreateRequest,
  MoneyTransaction,
  PagedRequest,
  PagedResponse,
  PayCreditInstallmentRequest,
  WithdrawalCreateRequest,
  JsonData,
} from "@/types";

export const myBankingService = {
  async getAccessibleCustomers() {
    const response = await api.get<JsonData<CustomerLookup[]>>("my-banking/customers");
    return unwrapCommonModel(response, "Вашите клиенти не бяха заредени");
  },

  async getProfile(customerId?: number) {
    const response = await api.get<JsonData<CustomerDetails>>("my-banking/profile", {
      params: customerId === undefined ? undefined : { customerId },
    });
    return unwrapCommonModel(response, "Профилът ви не бе зареден");
  },

  async getCredit(creditId: number) {
    const response = await api.get<JsonData<CreditDetails>>(`my-banking/credits/${creditId}`);
    return unwrapCommonModel(response, "Кредитът ви не бе зареден");
  },

  async requestDeposit(accountId: number, payload: DepositRequestCreateRequest) {
    const response = await api.post<JsonData<DepositRequest>>(
      `my-banking/accounts/${accountId}/deposit-requests`,
      payload,
    );
    return unwrapCommonModel(response, "Заявката за депозит не бе подадена");
  },

  async withdraw(accountId: number, payload: WithdrawalCreateRequest) {
    const response = await api.post<JsonData<AccountOperationResult>>(
      `my-banking/accounts/${accountId}/withdrawals`,
      payload,
    );
    return unwrapCommonModel(response, "Тегленето не бе извършено");
  },

  async payCreditInstallment(creditId: number, payload: PayCreditInstallmentRequest) {
    const response = await api.post<JsonData<CreditInstallmentPaymentResult>>(
      `my-banking/credits/${creditId}/pay-installment`,
      payload,
    );
    return unwrapCommonModel(response, "Вноската не бе платена");
  },

  async getAccountTransactions(accountId: number, request: PagedRequest) {
    const response = await api.get<JsonData<PagedResponse<MoneyTransaction>>>(
      `my-banking/accounts/${accountId}/transactions`,
      {
        params: {
          page: request.page,
          pageSize: request.pageSize,
        },
      },
    );
    return unwrapCommonModel(response, "Движенията по сметката не бяха заредени");
  },

  async getMyDepositRequests(customerId?: number) {
    const response = await api.get<JsonData<DepositRequest[]>>("my-banking/deposit-requests", {
      params: customerId === undefined ? undefined : { customerId },
    });
    return unwrapCommonModel(response, "Заявките ви за депозит не бяха заредени");
  },
};
