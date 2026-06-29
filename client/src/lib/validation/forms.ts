import { CreditType, CustomerType, RepresentativeRole } from "@/types";
import {
  type FieldErrors,
  calculatorLimits,
  emailText,
  maxLengthText,
  requiredIntegerInRange,
  requiredNumberInRange,
  requiredText,
} from "./rules";

// Повтаря границите за сума/срок от бекенда в CreateCreditRequest / CreateBankAccountRequest.
// Срокът ползва общия calculatorLimits (огледало на CalculatorLimits.cs), за да не се разминава с бекенда.
const minGrantedAmount = 0.01;
const maxMoneyAmount = 1_000_000_000;
const minOpeningBalance = 0;
const minTermMonths = calculatorLimits.minTermMonths;
const maxTermMonths = calculatorLimits.maxTermMonths;
const maxCustomerId = Number.MAX_SAFE_INTEGER;

// Тук проверяваме само формата (брой цифри) — пълната проверка на контролната сума на ЕГН/ЕИК остава на сървъра.
const egnPattern = /^\d{10}$/;
const eikPattern = /^(\d{9}|\d{13})$/;
const maxPersonNameLength = 100;
const maxCompanyNameLength = 200;

function isValidEnumValue(enumObject: Record<string, string | number>, value: number): boolean {
  return Object.values(enumObject).some((member) => typeof member === "number" && member === value);
}

export type CreditCreateFields = "customerId" | "creditType" | "grantedAmount" | "termMonths";

export function validateCreditCreate(values: {
  customerId: number;
  creditType: CreditType;
  grantedAmount: string;
  termMonths: string;
}): FieldErrors<CreditCreateFields> {
  return {
    customerId: requiredIntegerInRange(values.customerId, 1, maxCustomerId, "Клиент"),
    creditType: isValidEnumValue(CreditType, values.creditType) ? undefined : "Изберете валиден вид кредит.",
    grantedAmount: requiredNumberInRange(values.grantedAmount, minGrantedAmount, maxMoneyAmount, "Сума"),
    termMonths: requiredIntegerInRange(values.termMonths, minTermMonths, maxTermMonths, "Срок (месеци)"),
  };
}

export type AccountCreateFields = "customerId" | "openingBalance";

export function validateAccountCreate(values: {
  customerId: number;
  openingBalance: string;
}): FieldErrors<AccountCreateFields> {
  return {
    customerId: requiredIntegerInRange(values.customerId, 1, maxCustomerId, "Клиент"),
    openingBalance: requiredNumberInRange(values.openingBalance, minOpeningBalance, maxMoneyAmount, "Начално салдо"),
  };
}

// Повтаря границите [Range(0.01, 1000000000)] от заявките за операции с пари.
const minMoneyOperationAmount = 0.01;

export function validateMoneyAmount(amount: string): string | undefined {
  return requiredNumberInRange(amount, minMoneyOperationAmount, maxMoneyAmount, "Сума");
}

export type CustomerFormFields =
  | "customerType"
  | "firstName"
  | "lastName"
  | "personalIdentifier"
  | "companyName"
  | "companyIdentifier";

export type RepresentativeFields = "firstName" | "lastName" | "egn" | "role";

export function validateRepresentative(values: {
  firstName: string;
  lastName: string;
  egn: string;
  role: RepresentativeRole;
}): FieldErrors<RepresentativeFields> {
  return {
    firstName: requiredText(values.firstName, "Име") ?? maxLengthText(values.firstName, maxPersonNameLength, "Име"),
    lastName: requiredText(values.lastName, "Фамилия") ?? maxLengthText(values.lastName, maxPersonNameLength, "Фамилия"),
    egn: validateEgnShape(values.egn),
    role: isValidEnumValue(RepresentativeRole, values.role) ? undefined : "Изберете валидна роля.",
  };
}

export function validateCustomerForm(values: {
  customerType: CustomerType;
  firstName: string;
  lastName: string;
  personalIdentifier: string;
  companyName: string;
  companyIdentifier: string;
}): FieldErrors<CustomerFormFields> {
  if (!isValidEnumValue(CustomerType, values.customerType)) {
    return { customerType: "Изберете валиден вид клиент." };
  }

  if (values.customerType === CustomerType.Individual) {
    return {
      firstName:
        requiredText(values.firstName, "Име") ?? maxLengthText(values.firstName, maxPersonNameLength, "Име"),
      lastName:
        requiredText(values.lastName, "Фамилия") ?? maxLengthText(values.lastName, maxPersonNameLength, "Фамилия"),
      personalIdentifier: validateEgnShape(values.personalIdentifier),
    };
  }

  return {
    companyName:
      requiredText(values.companyName, "Име на фирма") ?? maxLengthText(values.companyName, maxCompanyNameLength, "Име на фирма"),
    companyIdentifier: validateEikShape(values.companyIdentifier),
  };
}

export type CounterUserFields =
  | "email"
  | "firstName"
  | "lastName"
  | "egn"
  | "companyName"
  | "companyIdentifier"
  | "representativeRole";

export function validateCounterUser(values: {
  email: string;
  customerType: CustomerType;
  firstName: string;
  lastName: string;
  egn: string;
  companyName: string;
  companyIdentifier: string;
  representativeRole: RepresentativeRole;
}): FieldErrors<CounterUserFields> {
  const base: FieldErrors<CounterUserFields> = {
    email: emailText(values.email),
    firstName: requiredText(values.firstName, "Име") ?? maxLengthText(values.firstName, maxPersonNameLength, "Име"),
    lastName: requiredText(values.lastName, "Фамилия") ?? maxLengthText(values.lastName, maxPersonNameLength, "Фамилия"),
    egn: validateEgnShape(values.egn),
  };

  if (values.customerType === CustomerType.Company) {
    base.companyName =
      requiredText(values.companyName, "Име на фирма") ?? maxLengthText(values.companyName, maxCompanyNameLength, "Име на фирма");
    base.companyIdentifier = validateEikShape(values.companyIdentifier);
    base.representativeRole = isValidEnumValue(RepresentativeRole, values.representativeRole)
      ? undefined
      : "Изберете валидна роля.";
  }

  return base;
}

function validateEgnShape(value: string): string | undefined {
  const requiredError = requiredText(value, "ЕГН");
  if (requiredError) {
    return requiredError;
  }
  return egnPattern.test(value.trim()) ? undefined : "ЕГН трябва да съдържа точно 10 цифри.";
}

function validateEikShape(value: string): string | undefined {
  const requiredError = requiredText(value, "ЕИК");
  if (requiredError) {
    return requiredError;
  }
  return eikPattern.test(value.trim()) ? undefined : "ЕИК трябва да съдържа точно 9 или 13 цифри.";
}
