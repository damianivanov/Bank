import { Check } from "lucide-react";
import { useId, useState, type ChangeEvent, type FormEvent } from "react";
import { CustomerType, RepresentativeRole, type RegisterCounterCustomerRequest } from "@/types";
import { Dropdown, TextInputField } from "@/shared/components";
import { representativeRoleLabels } from "@/lib/representativeRole";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateCounterUser, type CounterUserFields } from "@/lib/validation/forms";

type CounterUserFormProps = {
  isSubmitting: boolean;
  onSubmit: (value: RegisterCounterCustomerRequest) => Promise<void>;
};

function normalizeOptional(value: string): string | undefined {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : undefined;
}

export default function CounterUserForm({ isSubmitting, onSubmit }: CounterUserFormProps) {
  const [email, setEmail] = useState("");
  const [customerType, setCustomerType] = useState<CustomerType>(CustomerType.Individual);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [egn, setEgn] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [companyIdentifier, setCompanyIdentifier] = useState("");
  const [representativeRole, setRepresentativeRole] = useState<RepresentativeRole>(RepresentativeRole.Manager);
  const [errors, setErrors] = useState<FieldErrors<CounterUserFields>>({});
  const sectionId = useId();

  const isCompany = customerType === CustomerType.Company;

  const handleTypeChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const next = Number(event.target.value) as CustomerType;
    setCustomerType(next);
    if (next === CustomerType.Individual) {
      setCompanyName("");
      setCompanyIdentifier("");
    }
  };

  const buildValue = (): RegisterCounterCustomerRequest => {
    const common = {
      email: email.trim(),
      customerType,
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      egn: egn.trim(),
    };
    if (!isCompany) {
      return common;
    }
    return {
      ...common,
      companyName: normalizeOptional(companyName),
      companyIdentifier: normalizeOptional(companyIdentifier),
      representativeRole,
    };
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const fieldErrors = validateCounterUser({
      email,
      customerType,
      firstName,
      lastName,
      egn,
      companyName,
      companyIdentifier,
      representativeRole,
    });
    setErrors(fieldErrors);
    if (hasErrors(fieldErrors)) {
      return;
    }
    await onSubmit(buildValue());
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="min-w-0">
        <TextInputField
          label="Имейл (за вход)"
          name="email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          error={errors.email}
        />

        <div className="grid gap-4 md:grid-cols-2 mt-4">
          <Dropdown label="Вид клиент" value={customerType} onChange={handleTypeChange}>
            <option value={CustomerType.Individual}>Физическо лице</option>
            <option value={CustomerType.Company}>Юридическо лице</option>
          </Dropdown>

          {isCompany ? (
            <TextInputField
              label="ЕИК"
              name="companyIdentifier"
              value={companyIdentifier}
              onChange={(event) => setCompanyIdentifier(event.target.value)}
              inputMode="numeric"
              maxLength={13}
              pattern="\d{9}|\d{13}"
              title="ЕИК трябва да съдържа точно 9 или 13 цифри."
              error={errors.companyIdentifier}
            />
          ) : (
            <TextInputField
              label="ЕГН"
              name="egn"
              value={egn}
              onChange={(event) => setEgn(event.target.value)}
              inputMode="numeric"
              maxLength={10}
              pattern="\d{10}"
              title="ЕГН трябва да съдържа точно 10 цифри."
              error={errors.egn}
            />
          )}
        </div>
      </div>

      {isCompany ? (
        <>
          <TextInputField
            label="Име на фирма"
            name="companyName"
            value={companyName}
            onChange={(event) => setCompanyName(event.target.value)}
            error={errors.companyName}
          />

          <div
            role="group"
            aria-labelledby={`${sectionId}-rep`}
            className="min-w-0 border-t border-black/5 pt-5 dark:border-white/10"
          >
            <p id={`${sectionId}-rep`} className="mb-3 text-sm font-semibold text-secondary">
              Представител
            </p>
            <div className="grid gap-4 md:grid-cols-2 mt-4">
              <TextInputField
                label="Име"
                name="firstName"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                error={errors.firstName}
              />
              <TextInputField
                label="Фамилия"
                name="lastName"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                error={errors.lastName}
              />
              <TextInputField
                label="ЕГН"
                name="egn"
                value={egn}
                onChange={(event) => setEgn(event.target.value)}
                inputMode="numeric"
                maxLength={10}
                pattern="\d{10}"
                title="ЕГН трябва да съдържа точно 10 цифри."
                error={errors.egn}
              />
              <Dropdown
                label="Роля на представителя"
                value={representativeRole}
                onChange={(event) => setRepresentativeRole(Number(event.target.value) as RepresentativeRole)}
                error={errors.representativeRole}
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
        </>
      ) : (
        <div
          role="group"
          aria-labelledby={`${sectionId}-personal`}
          className="min-w-0 border-t border-black/5 pt-5 dark:border-white/10"
        >
          <p id={`${sectionId}-personal`} className="mb-3 text-sm font-semibold text-secondary">
            Лични данни
          </p>
          <div className="grid gap-4 md:grid-cols-2">
            <TextInputField
              label="Име"
              name="firstName"
              value={firstName}
              onChange={(event) => setFirstName(event.target.value)}
              error={errors.firstName}
            />
            <TextInputField
              label="Фамилия"
              name="lastName"
              value={lastName}
              onChange={(event) => setLastName(event.target.value)}
              error={errors.lastName}
            />
          </div>
        </div>
      )}

      <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-1 bank-btn disabled:opacity-60">
        <Check className="h-4 w-4" />
        {isSubmitting ? "Създаване..." : "Създай потребител"}
      </button>
    </form>
  );
}
