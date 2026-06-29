import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  CreditCalculatorRequest,
  CreditCalculatorResponse,
  JsonData,
  LeasingCalculatorRequest,
  LeasingCalculatorResponse,
  RefinancingCalculatorRequest,
  RefinancingCalculatorResponse,
} from "@/types";

export const calculatorService = {
  async calculateCredit(payload: CreditCalculatorRequest) {
    const response = await api.post<JsonData<CreditCalculatorResponse>>("calculators/credit", payload);
    return unwrapCommonModel(response, "Изчислението на кредита не бе успешно");
  },

  async calculateLeasing(payload: LeasingCalculatorRequest) {
    const response = await api.post<JsonData<LeasingCalculatorResponse>>("calculators/leasing", payload);
    return unwrapCommonModel(response, "Изчислението на лизинга не бе успешно");
  },

  async calculateRefinancing(payload: RefinancingCalculatorRequest) {
    const response = await api.post<JsonData<RefinancingCalculatorResponse>>("calculators/refinancing", payload);
    return unwrapCommonModel(response, "Изчислението на рефинансирането не бе успешно");
  },
};
