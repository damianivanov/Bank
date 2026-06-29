import type { JsonModels } from "../../backend";

export interface StaffUserPageModel
{
	items: JsonModels.Auth.StaffUserGridModel[];
	totalCount: number;
	page: number;
	pageSize: number;
	summary: JsonModels.Auth.StaffUserSummaryModel;
}
