import type { JsonModels } from "../../../backend";
import type { CreditStatus } from "../../Enums/CreditStatus";
import type { CreditType } from "../../Enums/CreditType";

export interface CreditDetailsModel
{
	id: number;
	customerId: number;
	customerDisplayName: string;
	creditType: CreditType;
	grantedAmount: number;
	termMonths: number;
	appliedAnnualInterestRate: number;
	appliedGrantingFee: number;
	customerWasVipAtCreation: boolean;
	plannedMonthlyPaymentAmount: number;
	currentAnnualInterestRate: number;
	status: CreditStatus;
	grantedAtUtc: string;
	repaidAtUtc?: string;
	lastPricingChange?: JsonModels.Bank.Credits.CreditPricingChangeModel;
	currentTerms?: JsonModels.Bank.Credits.CreditTermsModel;
	canPayNextInstallment: boolean;
	payments: JsonModels.Bank.Credits.CreditPaymentModel[];
}
