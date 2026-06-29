export interface ApiErrorModel
{
	id: number;
	dateCreated: string;
	message: string;
	details?: string;
	path?: string;
	userName?: string;
}
