import { RepresentativeRole } from "@/types";

export const representativeRoleLabels: Record<RepresentativeRole, string> = {
  [RepresentativeRole.Manager]: "Управител",
  [RepresentativeRole.Owner]: "Собственик",
  [RepresentativeRole.AuthorizedSignatory]: "Лице с право на подпис",
  [RepresentativeRole.Procurator]: "Прокурист",
};

export function formatRepresentativeRole(role: RepresentativeRole): string {
  return representativeRoleLabels[role] ?? "Представител";
}
