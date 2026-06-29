import { Check, Plus, X } from "lucide-react";
import { useState, type ChangeEvent, type FormEvent } from "react";
import { CustomerType, RepresentativeRole, type CreateCustomerRequest, type CustomerRepresentativeRequest } from "@/types";
import { Dropdown, TextInputField } from "@/shared/components";
import { representativeRoleLabels } from "@/lib/representativeRole";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import {
  validateCustomerForm,
  validateRepresentative,
  type CustomerFormFields,
  type RepresentativeFields,
} from "@/lib/validation/forms";

export type CustomerFormValue = CreateCustomerRequest;

type CustomerFormProps = {
  initialValue: CustomerFormValue;
  submitLabel: string;
  isSubmitting: boolean;
  showPanel?: boolean;
  onSubmit: (value: CustomerFormValue) => Promise<void>;
};

type RepresentativeRow = {
  firstName: string;
  lastName: string;
  egn: string;
  role: RepresentativeRole;
  validFrom?: string;
  validTo?: string;
};

function normalizeOptional(value: string): string | undefined {
  const trimmedValue = value.trim();
  return trimmedValue.length > 0 ? trimmedValue : undefined;
}

function createEmptyRepresentative(): RepresentativeRow {
  return { firstName: "", lastName: "", egn: "", role: RepresentativeRole.Manager };
}

function toRepresentativeRows(representatives: readonly CustomerRepresentativeRequest[] | undefined): RepresentativeRow[] {
  if (!representatives || representatives.length === 0) {
    return [createEmptyRepresentative()];
  }

  return representatives.map((representative) => ({
    firstName: representative.firstName ?? "",
    lastName: representative.lastName ?? "",
    egn: representative.egn ?? "",
    role: representative.role,
    validFrom: representative.validFrom,
    validTo: representative.validTo,
  }));
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
  const [representatives, setRepresentatives] = useState<RepresentativeRow[]>(() => toRepresentativeRows(initialValue.representatives));
  const [errors, setErrors] = useState<FieldErrors<CustomerFormFields>>({});
  const [representativeErrors, setRepresentativeErrors] = useState<FieldErrors<RepresentativeFields>[]>([]);

  const handleCustomerTypeChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const nextCustomerType = Number(event.target.value) as CustomerType;
    setCustomerType(nextCustomerType);

    if (nextCustomerType === CustomerType.Individual) {
      setCompanyName("");
      setCompanyIdentifier("");
      setRepresentatives([createEmptyRepresentative()]);
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
    }
  };

  const updateRepresentative = (index: number, patch: Partial<RepresentativeRow>) => {
    setRepresentatives((current) => current.map((row, rowIndex) => (rowIndex === index ? { ...row, ...patch } : row)));
  };

  const handleAddRepresentative = () => {
    setRepresentatives((current) => [...current, createEmptyRepresentative()]);
  };

  const handleRemoveRepresentative = (index: number) => {
    setRepresentatives((current) => (current.length <= 1 ? current : current.filter((_, rowIndex) => rowIndex !== index)));
  };

  const isIndividual = customerType === CustomerType.Individual;

  const buildSubmitValue = (): CustomerFormValue => {
    if (isIndividual) {
      return {
        customerType,
        firstName: normalizeOptional(firstName),
        lastName: normalizeOptional(lastName),
        personalIdentifier: normalizeOptional(personalIdentifier),
      } satisfies CustomerFormValue;
    }

    return {
      customerType,
      companyName: normalizeOptional(companyName),
      companyIdentifier: normalizeOptional(companyIdentifier),
      representatives: representatives.map((row) => ({
        firstName: row.firstName.trim(),
        lastName: row.lastName.trim(),
        egn: row.egn.trim(),
        role: row.role,
        validFrom: row.validFrom,
        validTo: row.validTo,
      })),
    } satisfies CustomerFormValue;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const fieldErrors = validateCustomerForm({
      customerType,
      firstName,
      lastName,
      personalIdentifier,
      companyName,
      companyIdentifier,
    });
    setErrors(fieldErrors);

    const repErrors = isIndividual
      ? []
      : representatives.map((representative) =>
          validateRepresentative({
            firstName: representative.firstName,
            lastName: representative.lastName,
            egn: representative.egn,
            role: representative.role,
          }),
        );
    setRepresentativeErrors(repErrors);

    if (hasErrors(fieldErrors) || repErrors.some((rowErrors) => hasErrors(rowErrors))) {
      return;
    }

    await onSubmit(buildSubmitValue());
  };

  const formClassName = showPanel ? "bank-panel mt-6 rounded-2xl p-5" : "space-y-5";

  return (
    <form onSubmit={handleSubmit} className={formClassName}>
      <div className="grid gap-4 md:grid-cols-2">
        <Dropdown
          label="Вид клиент"
          value={customerType}
          onChange={handleCustomerTypeChange}
          className="md:col-span-2"
        >
          <option value={CustomerType.Individual}>Физическо лице</option>
          <option value={CustomerType.Company}>Юридическо лице</option>
        </Dropdown>

        {isIndividual ? (
          <>
            <TextInputField
              label="Име"
              name="firstName"
              value={firstName}
              onChange={handleFieldChange}
              error={errors.firstName}
            />
            <TextInputField
              label="Фамилия"
              name="lastName"
              value={lastName}
              onChange={handleFieldChange}
              error={errors.lastName}
            />
            <TextInputField
              label="ЕГН"
              name="personalIdentifier"
              value={personalIdentifier}
              onChange={handleFieldChange}
              inputMode="numeric"
              maxLength={10}
              pattern="\d{10}"
              title="ЕГН трябва да съдържа точно 10 цифри."
              className="md:col-span-2"
              error={errors.personalIdentifier}
            />
          </>
        ) : (
          <>
            <TextInputField
              label="ЕИК"
              name="companyIdentifier"
              value={companyIdentifier}
              onChange={handleFieldChange}
              inputMode="numeric"
              maxLength={13}
              pattern="\d{9}|\d{13}"
              title="ЕИК трябва да съдържа точно 9 или 13 цифри."
              className="md:col-span-2"
              error={errors.companyIdentifier}
            />
            
            <TextInputField
              label="Име на фирма"
              name="companyName"
              value={companyName}
              onChange={handleFieldChange}
              className="md:col-span-2"
              error={errors.companyName}
            />
            <div className="md:col-span-2">
              <div className="my-8 flex items-center justify-between">
                <p className="text-sm font-semibold">Представители</p>
                <button
                  type="button"
                  onClick={handleAddRepresentative}
                  className="bank-secondary-btn bank-btn-action"
                >
                  <Plus className="h-3.5 w-3.5" />
                  Добави представител
                </button>
              </div>

              <div className="space-y-4">
                {representatives.map((representative, index) => (
                  <div key={index} className="bank-panel rounded-xl p-4">
                    <div className="mb-2 flex items-center justify-between">
                      <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Представител {index + 1}</p>
                      {representatives.length > 1 ? (
                        <button
                          type="button"
                          onClick={() => handleRemoveRepresentative(index)}
                          className="inline-flex cursor-pointer items-center gap-1 text-xs font-semibold text-rose-500 transition-colors hover:text-rose-600"
                        >
                          <X className="h-3.5 w-3.5" />
                          Премахни
                        </button>
                      ) : null}
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                      <TextInputField
                        label="Име"
                        value={representative.firstName}
                        onChange={(event) => updateRepresentative(index, { firstName: event.target.value })}
                        error={representativeErrors[index]?.firstName}
                      />
                      <TextInputField
                        label="Фамилия"
                        value={representative.lastName}
                        onChange={(event) => updateRepresentative(index, { lastName: event.target.value })}
                        error={representativeErrors[index]?.lastName}
                      />
                      <TextInputField
                        label="ЕГН"
                        value={representative.egn}
                        onChange={(event) => updateRepresentative(index, { egn: event.target.value })}
                        inputMode="numeric"
                        maxLength={10}
                        pattern="\d{10}"
                        title="ЕГН трябва да съдържа точно 10 цифри."
                        error={representativeErrors[index]?.egn}
                      />
                      <Dropdown
                        label="Роля"
                        value={representative.role}
                        onChange={(event) => updateRepresentative(index, { role: Number(event.target.value) as RepresentativeRole })}
                        error={representativeErrors[index]?.role}
                      >
                        {Object.values(RepresentativeRole)
                          .filter((value): value is RepresentativeRole => typeof value === "number")
                          .map((role) => (
                            <option key={role} value={role}>
                              {representativeRoleLabels[role]}
                            </option>
                          ))}
                      </Dropdown>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </>
        )}
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="bank-primary-btn mt-5 bank-btn disabled:opacity-60"
      >
        <Check className="h-4 w-4" />
        {isSubmitting ? "Запазване..." : submitLabel}
      </button>
    </form>
  );
}
