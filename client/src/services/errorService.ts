import api from "@/lib/api";
import { unwrapCommonModel } from "@/lib/commonModel";
import type { ApiError, JsonData, PagedRequest, PagedResponse } from "@/types";

export const errorService = {
  async getErrors(request: PagedRequest, fromDate?: string, toDate?: string) {
    const search = request.search?.trim();
    const response = await api.get<JsonData<PagedResponse<ApiError>>>("admin/errors", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: search ? search : undefined,
        fromDate: fromDate ? fromDate : undefined,
        toDate: toDate ? toDate : undefined,
      },
    });
    return unwrapCommonModel(response, "Грешките не бяха заредени");
  },
};
