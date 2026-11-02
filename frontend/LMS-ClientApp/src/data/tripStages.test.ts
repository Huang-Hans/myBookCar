import { describe, expect, it } from 'vitest'
import { customerStageTimes, customerTripStageIndex, customerTripStages } from './tripStages'

describe('customer trip stages', () => {
  it('uses the same fixed progression as the driver active-trip stages', () => {
    expect(customerTripStages).toEqual([
      'En Route to Pickup', 'Arrived at Pickup', 'Passenger Boarded', 'In Transit', 'Arrived at Destination', 'Completed',
    ])
  })

  it('maps each trip stage to a single progress position without duplicates', () => {
    expect(customerTripStageIndex('DRIVER_EN_ROUTE')).toBe(0)
    expect(customerTripStageIndex('EN_ROUTE_TO_PICKUP')).toBe(0)
    expect(customerTripStageIndex('ARRIVED_AT_PICKUP')).toBe(1)
    expect(customerTripStageIndex('PASSENGER_ON_BOARD')).toBe(2)
    expect(customerTripStageIndex('IN_TRANSIT')).toBe(3)
    expect(customerTripStageIndex('ARRIVED_AT_DESTINATION')).toBe(4)
    expect(customerTripStageIndex('COMPLETED')).toBe(5)
  })

  it('treats not-yet-started or unknown stages as fully pending', () => {
    expect(customerTripStageIndex('PENDING')).toBe(-1)
    expect(customerTripStageIndex('CONFIRMED')).toBe(-1)
    expect(customerTripStageIndex('DRIVER_ASSIGNED')).toBe(-1)
    expect(customerTripStageIndex('unknown')).toBe(-1)
  })

  it('attaches each stage time node from the first matching timeline event', () => {
    const times = customerStageTimes([
      { type: 'STARTED_EN_ROUTE', occurredAt: '2026-10-22T08:05:00Z' },
      { type: 'ARRIVED_AT_PICKUP', occurredAt: '2026-10-22T08:20:00Z' },
      { type: 'PASSENGER_BOARDED', occurredAt: '2026-10-22T08:30:00Z' },
      { type: 'DRIVER_EN_ROUTE', occurredAt: '2026-10-22T08:40:00Z' },
    ])
    expect(times).toEqual([
      '2026-10-22T08:05:00Z', '2026-10-22T08:20:00Z', '2026-10-22T08:30:00Z', null, null, null,
    ])
  })

  it('leaves stages without events and non-stage events without time nodes', () => {
    const times = customerStageTimes([
      { type: 'DRIVER_ASSIGNED', occurredAt: '2026-10-22T07:00:00Z' },
      { type: 'COMPLETED', occurredAt: '2026-10-22T09:30:00Z' },
    ])
    expect(times).toEqual([null, null, null, null, null, '2026-10-22T09:30:00Z'])
  })
})
