export interface JsonData<T>
{
	success: boolean;
	error?: string;
	warning?: string;
	data?: T;
}
