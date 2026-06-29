const GROUP_SEPARATOR = " ";

/**
 * Премахва всичко, което не е цифра или десетична точка, и слива излишните
 * десетични точки, връщайки суровия числов низ (напр. "310000.5"). Точно това
 * пазим в state-а, за да продължат да работят `Number(value)` и валидацията.
 */
export function cleanNumberInput(value: string): string {
  let cleaned = value.replace(/[^\d.]/g, "");
  const firstDot = cleaned.indexOf(".");
  if (firstDot !== -1) {
    cleaned = cleaned.slice(0, firstDot + 1) + cleaned.slice(firstDot + 1).replace(/\./g, "");
  }
  // Реже водещите нули в цялата част ("00050" -> "50"), но запазва една нула
  // за десетичните стойности ("0.5") и за самостоятелната нула ("0").
  cleaned = cleaned.replace(/^0+(?=\d)/, "");
  return cleaned;
}

/**
 * Форматира суровия числов низ за показване, като групира цялата част по
 * хиляди (напр. "310000.5" -> "310 000.5"). Десетичният разделител остава
 * точка, така че показаната стойност пак е валидно число след премахване на
 * интервалите.
 */
export function formatNumberInput(raw: string): string {
  if (raw === "") {
    return "";
  }

  const dotIndex = raw.indexOf(".");
  const intPart = dotIndex === -1 ? raw : raw.slice(0, dotIndex);
  const groupedInt = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, GROUP_SEPARATOR);

  return dotIndex === -1 ? groupedInt : `${groupedInt}.${raw.slice(dotIndex + 1)}`;
}
