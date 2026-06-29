import NumberInputField, { type NumberInputFieldProps } from "./NumberInputField";

type MoneyInputFieldProps = Omit<NumberInputFieldProps, "suffix">;

export default function MoneyInputField(props: MoneyInputFieldProps) {
  return <NumberInputField suffix="€" {...props} />;
}
