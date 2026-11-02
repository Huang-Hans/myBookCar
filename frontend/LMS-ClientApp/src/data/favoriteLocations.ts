export interface FavoriteLocations {
  pickupLocation: string
  dropoffLocation: string
}

export interface FavoriteLocationStorage {
  getStorageSync(key: string): unknown
  setStorageSync(key: string, value: unknown): void
}

export const favoriteLocationsStorageKey = 'lmsClientFavoriteLocations'

export const defaultFavoriteLocations: Readonly<FavoriteLocations> = Object.freeze({
  pickupLocation: '',
  dropoffLocation: '',
})

function normalizedText(value: unknown, fallback: string): string {
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

export function parseFavoriteLocations(value: unknown): FavoriteLocations {
  if (!value || typeof value !== 'object') return { ...defaultFavoriteLocations }
  const candidate = value as Partial<FavoriteLocations>
  return {
    pickupLocation: normalizedText(candidate.pickupLocation, defaultFavoriteLocations.pickupLocation),
    dropoffLocation: normalizedText(candidate.dropoffLocation, defaultFavoriteLocations.dropoffLocation),
  }
}

export function loadFavoriteLocations(storage: FavoriteLocationStorage = uni): FavoriteLocations {
  return parseFavoriteLocations(storage.getStorageSync(favoriteLocationsStorageKey))
}

export function saveFavoriteLocations(
  value: FavoriteLocations,
  storage: FavoriteLocationStorage = uni,
): FavoriteLocations {
  const pickupLocation = value.pickupLocation.trim()
  const dropoffLocation = value.dropoffLocation.trim()
  if (!pickupLocation || !dropoffLocation) throw new Error('Both favorite locations are required.')
  const normalized = { pickupLocation, dropoffLocation }
  storage.setStorageSync(favoriteLocationsStorageKey, normalized)
  return normalized
}
