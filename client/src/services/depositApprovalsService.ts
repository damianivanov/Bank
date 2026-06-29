import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  AccountOperationResult,
  DepositRejectRequest,
  DepositRequestQueue,
  DepositRequestStatus,
  JsonData,
  PagedRequest,
  PagedResponse,
} from "@/types";

export const depositApprovalsService = {
  async getDepositRequests(status: DepositRequestStatus | undefined, request: PagedRequest) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<PagedResponse<DepositRequestQueue>>>("deposit-requests", {
      params: {
        status,
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
      },
    });
    return unwrapCommonModel(response, "Заявките за депозит не бяха заредени");
  },

  async approve(depositRequestId: number) {
    const response = await api.post<JsonData<AccountOperationResult>>(
      `deposit-requests/${depositRequestId}/approve`,
      {},
    );
    return unwrapCommonModel(response, "Заявката не бе одобрена");
  },

  async reject(depositRequestId: number, payload: DepositRejectRequest) {
    const response = await api.post<JsonData<DepositRequestQueue>>(
      `deposit-requests/${depositRequestId}/reject`,
      payload,
    );
    return unwrapCommonModel(response, "Заявката не бе отхвърлена");
  },
};
