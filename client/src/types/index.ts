export * from "./JsonModels";

import type { JsonModels } from "./backend";

export type BankAccount = JsonModels.Bank.Accounts.BankAccountModel;
export type BankAccountDetails = JsonModels.Bank.Accounts.BankAccountDetailsModel;
export type Credit = JsonModels.Bank.Credits.CreditModel;
export type CreditDetails = JsonModels.Bank.Credits.CreditDetailsModel;
export type CreditPayment = JsonModels.Bank.Credits.CreditPaymentModel;
export type CreditPricingChange = JsonModels.Bank.Credits.CreditPricingChangeModel;
export type CreditTypeCondition = JsonModels.Bank.CreditConditions.CreditTypeConditionModel;
export type Customer = JsonModels.Bank.Customers.CustomerModel;
export type CustomerAccountSummary = JsonModels.Bank.Customers.CustomerAccountSummaryModel;
export type CustomerCreditSummary = JsonModels.Bank.Customers.CustomerCreditSummaryModel;
export type CustomerDetails = JsonModels.Bank.Customers.CustomerDetailsModel;
export type CustomerLookup = JsonModels.Bank.Customers.CustomerLookupModel;
export type StaffUserGrid = JsonModels.Auth.StaffUserGridModel;
export type User = JsonModels.Auth.UserModel;
export type UserAccess = JsonModels.Auth.UserAccessModel;
