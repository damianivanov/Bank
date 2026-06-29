import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type {
  JsonData,
  SaveCalculationRequest,
  SavedCalculationDetailsModel,
  SavedCalculationModel,
} from "@/types";

export const savedCalculationService = {
  async list() {
    const response = await api.get<JsonData<SavedCalculationModel[]>>("saved-calculations");
    return unwrapCommonModel(response, "Запазените изчисления не бяха заредени");
  },

  async get(id: number) {
    const response = await api.get<JsonData<SavedCalculationDetailsModel>>(`saved-calculations/${id}`);
    return unwrapCommonModel(response, "Запазеното изчисление не бе заредено");
  },

  async save(payload: SaveCalculationRequest) {
    const response = await api.post<JsonData<SavedCalculationModel>>("saved-calculations", payload);
    return unwrapCommonModel(response, "Изчислението не бе запазено");
  },

  async update(id: number, payload: SaveCalculationRequest) {
    const response = await api.put<JsonData<SavedCalculationModel>>(`saved-calculations/${id}`, payload);
    return unwrapCommonModel(response, "Изчислението не бе обновено");
  },

  async remove(id: number) {
    const response = await api.delete<JsonData<string>>(`saved-calculations/${id}`);
    return unwrapCommonModel(response, "Запазеното изчисление не бе изтрито");
  },
};
