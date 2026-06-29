import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreditTypeCondition,
  JsonData,
  PublicCreditCondition,
  UpdateCreditConditionRequest,
} from "@/types";

export const creditConditionService = {
  async getCreditConditions() {
    const response = await api.get<JsonData<CreditTypeCondition[]>>("credit-conditions");
    return unwrapCommonModel(response, "Кредитните условия не бяха заредени");
  },

  async getPublicCreditConditions() {
    const response = await api.get<JsonData<PublicCreditCondition[]>>("credit-conditions/public");
    return unwrapCommonModel(response, "Кредитните условия не бяха заредени");
  },

  async updateCreditCondition(id: number, payload: UpdateCreditConditionRequest) {
    const response = await api.put<JsonData<CreditTypeCondition>>(`credit-conditions/${id}`, payload);
    return unwrapCommonModel(response, "Кредитните условия не бяха обновени");
  },
};
