export interface JsonData<T>
{
	success: boolean;
	error?: string;
	data?: T;
}
