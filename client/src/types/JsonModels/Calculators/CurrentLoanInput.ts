export interface CurrentLoanInput
{
	principal: number;
	annualRatePercent: number;
	termMonths: number;
	paymentsMade: number;
	prepaymentFeePercent: number;
}
