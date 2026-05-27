import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreateCreditRequest,
  Credit,
  CreditDetails,
  JsonData,
} from "@/types";

export const creditService = {
  async getCredits() {
    const response = await api.get<JsonData<Credit[]>>("credits");
    return unwrapCommonModel(response, "Could not load credits");
  },

  async getCredit(creditId: number) {
    const response = await api.get<JsonData<CreditDetails>>(`credits/${creditId}`);
    return unwrapCommonModel(response, "Could not load credit");
  },

  async createCredit(payload: CreateCreditRequest) {
    const response = await api.post<JsonData<CreditDetails>>("credits", payload);
    return unwrapCommonModel(response, "Could not grant credit");
  },

  async payPayment(creditId: number, paymentId: number) {
    const response = await api.post<JsonData<CreditDetails>>(`credits/${creditId}/payments/${paymentId}/pay`, {});
    return unwrapCommonModel(response, "Could not pay payment");
  },
};
