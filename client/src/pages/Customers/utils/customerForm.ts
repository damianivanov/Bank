import { CustomerType, type CreateCustomerRequest, type CustomerDetails, type CustomerEdit } from "@/types";

export type LinkUserContext = {
  linkUserId: number;
  linkUserEmail?: string;
  linkUserFirstName?: string;
  linkUserLastName?: string;
};

export function mapCustomerToFormValue(customer: CustomerEdit): CreateCustomerRequest {
  const model: CreateCustomerRequest = {
    customerType: customer.customerType,
    firstName: customer.firstName,
    lastName: customer.lastName,
    personalIdentifier: customer.personalIdentifier,
    companyName: customer.companyName,
    companyIdentifier: customer.companyIdentifier,
    representatives: customer.representatives.map((representative) => ({
      firstName: representative.firstName,
      lastName: representative.lastName,
      egn: representative.egn,
      role: representative.role,
      validFrom: representative.validFrom,
      validTo: representative.validTo,
    })),
  };
  return model;
}

export function buildLinkedUserDisplayName(linkUserContext: LinkUserContext): string {
  const fullName = [linkUserContext.linkUserFirstName, linkUserContext.linkUserLastName]
    .filter(Boolean)
    .join(" ")
    .trim();

  if (fullName) {
    return linkUserContext.linkUserEmail ? `${fullName} (${linkUserContext.linkUserEmail})` : fullName;
  }

  return linkUserContext.linkUserEmail ?? `Потребител №${linkUserContext.linkUserId}`;
}

export function getCustomerDisplayName(customer: CustomerDetails): string {
  return customer.customerType === CustomerType.Individual
    ? `${customer.firstName ?? ""} ${customer.lastName ?? ""}`.trim()
    : customer.companyName ?? "";
}
