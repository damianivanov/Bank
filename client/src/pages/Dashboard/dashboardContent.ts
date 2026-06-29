export function getGreeting(hour: number): string {
  if (hour >= 5 && hour < 12) {
    return "Добро утро";
  }
  if (hour >= 12 && hour < 18) {
    return "Добър ден";
  }
  if (hour >= 18 && hour < 23) {
    return "Добър вечер";
  }
  return "Здравейте";
}

export const quickActionDescriptions: Record<string, string> = {
  "/my-banking": "Вашите сметки, кредити и операции.",
  "/calculators": "Изчислете вноска и оскъпяване по кредит.",
  "/all-users": "Преглед и управление на потребителите.",
  "/customers": "Клиентски профили и данни.",
  "/management/deposit-approvals": "Одобрение на заявените депозити.",
  "/management/credit-conditions": "Тарифи и условия по кредитите.",
  "/management/users": "Роли и достъп на потребителите.",
};
