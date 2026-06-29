import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreateCreditRequest,
  Credit,
  CreditDetails,
  JsonData,
  PagedRequest,
  PagedResponse,
} from "@/types";

export const creditService = {
  async getCredits(request: PagedRequest) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<PagedResponse<Credit>>>("credits", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
      },
    });
    return unwrapCommonModel(response, "Кредитите не бяха заредени");
  },

  async getCredit(creditId: number) {
    const response = await api.get<JsonData<CreditDetails>>(`credits/${creditId}`);
    return unwrapCommonModel(response, "Кредитът не бе зареден");
  },

  async createCredit(payload: CreateCreditRequest) {
    const response = await api.post<JsonData<CreditDetails>>("credits", payload);
    return unwrapCommonModel(response, "Кредитът не бе отпуснат");
  },
};
