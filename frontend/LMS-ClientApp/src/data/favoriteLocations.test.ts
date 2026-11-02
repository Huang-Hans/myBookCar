import { describe, expect, it, vi } from 'vitest'
import {
  defaultFavoriteLocations,
  favoriteLocationsStorageKey,
  loadFavoriteLocations,
  saveFavoriteLocations,
  type FavoriteLocationStorage,
} from './favoriteLocations'

function storageWith(value: unknown): FavoriteLocationStorage {
  return {
    getStorageSync: vi.fn(() => value),
    setStorageSync: vi.fn(),
  }
}

describe('favorite locations', () => {
  it('has no quick favorites until the customer saves them', () => {
    expect(defaultFavoriteLocations).toEqual({ pickupLocation: '', dropoffLocation: '' })
    expect(loadFavoriteLocations(storageWith(null))).toEqual(defaultFavoriteLocations)
  })

  it('loads valid stored locations and repairs missing values', () => {
    const storage = storageWith({ pickupLocation: '  Studio City  ', dropoffLocation: '' })
    expect(loadFavoriteLocations(storage)).toEqual({
      pickupLocation: 'Studio City',
      dropoffLocation: defaultFavoriteLocations.dropoffLocation,
    })
  })

  it('trims and persists both favorite locations', () => {
    const storage = storageWith(null)
    expect(saveFavoriteLocations({ pickupLocation: '  Nuwa  ', dropoffLocation: ' Morpheus ' }, storage)).toEqual({
      pickupLocation: 'Nuwa',
      dropoffLocation: 'Morpheus',
    })
    expect(storage.setStorageSync).toHaveBeenCalledWith(favoriteLocationsStorageKey, {
      pickupLocation: 'Nuwa',
      dropoffLocation: 'Morpheus',
    })
  })

  it('rejects incomplete settings without writing them', () => {
    const storage = storageWith(null)
    expect(() => saveFavoriteLocations({ pickupLocation: '', dropoffLocation: 'Morpheus' }, storage)).toThrow(
      'Both favorite locations are required.',
    )
    expect(storage.setStorageSync).not.toHaveBeenCalled()
  })
})
