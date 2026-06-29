export interface LeasingCalculatorResponse
{
	totalCost: number;
	totalMarkup: number;
	markupPercentage: number;
	effectiveInterestRate: number;
	processingFeeAmount: number;
	totalPaid: number;
}
