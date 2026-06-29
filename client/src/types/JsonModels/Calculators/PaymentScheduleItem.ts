export interface PaymentScheduleItem
{
	month: number;
	date: string;
	payment: number;
	principal: number;
	interest: number;
	remainingBalance: number;
	fees: number;
	cashFlow: number;
}
