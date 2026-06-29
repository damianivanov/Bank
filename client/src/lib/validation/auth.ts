import { type FieldErrors, emailText, minLengthText, passwordMinLength, requiredText } from "./rules";

export type LoginFields = "email" | "password";
export type RegisterFields = "firstName" | "lastName" | "email" | "password";

export function validateLogin(values: { email: string; password: string }): FieldErrors<LoginFields> {
  return {
    email: emailText(values.email),
    password: requiredText(values.password, "Парола"),
  };
}

export function validateRegister(values: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}): FieldErrors<RegisterFields> {
  return {
    firstName: requiredText(values.firstName, "Име"),
    lastName: requiredText(values.lastName, "Фамилия"),
    email: emailText(values.email),
    password: minLengthText(values.password, passwordMinLength, "Парола"),
  };
}

export type ChangePasswordFields = "currentPassword" | "newPassword" | "confirmPassword";

export function validateChangePassword(values: {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}): FieldErrors<ChangePasswordFields> {
  return {
    currentPassword: requiredText(values.currentPassword, "Текуща парола"),
    newPassword: minLengthText(values.newPassword, passwordMinLength, "Нова парола"),
    confirmPassword:
      requiredText(values.confirmPassword, "Потвърждение") ??
      (values.newPassword !== values.confirmPassword ? "Паролите не съвпадат." : undefined),
  };
}
