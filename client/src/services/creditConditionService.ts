import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type { CreditTypeCondition, JsonData } from "@/types";

export const creditConditionService = {
  async getCreditConditions() {
    const response = await api.get<JsonData<CreditTypeCondition[]>>("credit-conditions");
    return unwrapCommonModel(response, "Could not load credit conditions");
  },
};
