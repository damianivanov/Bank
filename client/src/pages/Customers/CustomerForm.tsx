import { useState, type ChangeEvent, type FormEvent } from "react";
import { CustomerType, type CreateCustomerRequest } from "@/types";
import { Dropdown, TextInputField } from "@/shared/components";

export type CustomerFormValue = CreateCustomerRequest;

type CustomerFormProps = {
  initialValue: CustomerFormValue;
  submitLabel: string;
  isSubmitting: boolean;
  showPanel?: boolean;
  onSubmit: (value: CustomerFormValue) => Promise<void>;
};

function normalizeOptional(value: string): string | undefined {
  const trimmedValue = value.trim();
  return trimmedValue.length > 0 ? trimmedValue : undefined;
}

export default function CustomerForm({
  initialValue,
  submitLabel,
  isSubmitting,
  showPanel = true,
  onSubmit,
}: CustomerFormProps) {
  const [customerType, setCustomerType] = useState(initialValue.customerType);
  const [firstName, setFirstName] = useState(initialValue.firstName ?? "");
  const [lastName, setLastName] = useState(initialValue.lastName ?? "");
  const [personalIdentifier, setPersonalIdentifier] = useState(initialValue.personalIdentifier ?? "");
  const [companyName, setCompanyName] = useState(initialValue.companyName ?? "");
  const [companyIdentifier, setCompanyIdentifier] = useState(initialValue.companyIdentifier ?? "");
  const [representativeName, setRepresentativeName] = useState(initialValue.representativeName ?? "");

  const handleCustomerTypeChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const nextCustomerType = Number(event.target.value) as CustomerType;
    setCustomerType(nextCustomerType);

    if (nextCustomerType === CustomerType.Individual) {
      setCompanyName("");
      setCompanyIdentifier("");
      setRepresentativeName("");
      return;
    }

    setFirstName("");
    setLastName("");
    setPersonalIdentifier("");
  };

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;

    if (name === "firstName") {
      setFirstName(value);
      return;
    }

    if (name === "lastName") {
      setLastName(value);
      return;
    }

    if (name === "personalIdentifier") {
      setPersonalIdentifier(value);
      return;
    }

    if (name === "companyName") {
      setCompanyName(value);
      return;
    }

    if (name === "companyIdentifier") {
      setCompanyIdentifier(value);
      return;
    }

    setRepresentativeName(value);
  };

  const handleSubmitValue = {
    customerType,
    firstName: normalizeOptional(firstName),
    lastName: normalizeOptional(lastName),
    personalIdentifier: normalizeOptional(personalIdentifier),
    companyName: normalizeOptional(companyName),
    companyIdentifier: normalizeOptional(companyIdentifier),
    representativeName: normalizeOptional(representativeName),
  } satisfies CustomerFormValue;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onSubmit(handleSubmitValue);
  };

  const isIndividual = customerType === CustomerType.Individual;
  const formClassName = showPanel ? "bank-panel mt-6 rounded-2xl p-5" : "space-y-5";

  return (
    <form onSubmit={handleSubmit} className={formClassName}>
      <div className="grid gap-4 md:grid-cols-2">
        <Dropdown
          label="Customer type"
          value={customerType}
          onChange={handleCustomerTypeChange}
          className="md:col-span-2"
        >
          <option value={CustomerType.Individual}>Individual</option>
          <option value={CustomerType.Company}>Company</option>
        </Dropdown>

        {isIndividual ? (
          <>
            <TextInputField
              label="First name"
              name="firstName"
              value={firstName}
              onChange={handleFieldChange}
              required
            />
            <TextInputField
              label="Last name"
              name="lastName"
              value={lastName}
              onChange={handleFieldChange}
              required
            />
            <TextInputField
              label="Personal identifier (EGN)"
              name="personalIdentifier"
              value={personalIdentifier}
              onChange={handleFieldChange}
              inputMode="numeric"
              maxLength={10}
              pattern="\d{10}"
              title="EGN must contain exactly 10 digits."
              required
              className="md:col-span-2"
            />
          </>
        ) : (
          <>
            <TextInputField
              label="Company name"
              name="companyName"
              value={companyName}
              onChange={handleFieldChange}
              required
              className="md:col-span-2"
            />
            <TextInputField
              label="Company identifier (EIK)"
              name="companyIdentifier"
              value={companyIdentifier}
              onChange={handleFieldChange}
              inputMode="numeric"
              maxLength={13}
              pattern="\d{9}|\d{13}"
              title="EIK must contain exactly 9 or 13 digits."
              required
            />
            <TextInputField
              label="Representative"
              name="representativeName"
              value={representativeName}
              onChange={handleFieldChange}
              required
            />
          </>
        )}
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
      >
        {isSubmitting ? "Saving..." : submitLabel}
      </button>
    </form>
  );
}
