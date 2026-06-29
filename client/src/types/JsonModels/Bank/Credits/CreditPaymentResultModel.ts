import type { JsonModels } from "../../../backend";
import type { CreditStatus } from "../../Enums/CreditStatus";

export interface CreditPaymentResultModel
{
	creditId: number;
	creditStatus: CreditStatus;
	creditRepaidAtUtc?: string;
	payment: JsonModels.Bank.Credits.CreditPaymentModel;
}
