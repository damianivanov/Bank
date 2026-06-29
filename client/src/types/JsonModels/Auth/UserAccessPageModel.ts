import type { JsonModels } from "../../backend";

export interface UserAccessPageModel
{
	items: JsonModels.Auth.UserAccessModel[];
	totalCount: number;
	page: number;
	pageSize: number;
	summary: JsonModels.Auth.UserAccessSummaryModel;
}
