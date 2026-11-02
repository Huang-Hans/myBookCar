export function resolveCitySelection(
  availableCities: readonly string[],
  requestedCity: string,
  currentCity: string,
): string {
  return availableCities.includes(requestedCity) ? requestedCity : currentCity
}
