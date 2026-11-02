import { describe, expect, it } from 'vitest'
import { activeTripStages, canReceiveRequest, createInspectionAnswers, inspectionItemsFor, isInspectionComplete, nextTripStage, progressActionLabel, stageTimesFor, tripStageIndex } from './driverState'

describe('driver preview state', () => {
  it('requires an answer for every inspection item', () => {
    expect(isInspectionComplete(['Tyres', 'Lights'], { Tyres: 'Pass', Lights: null })).toBe(false)
    expect(isInspectionComplete(['Tyres', 'Lights'], { Tyres: 'Pass', Lights: 'Fail' })).toBe(true)
  })

  it('starts every inspection control unselected', () => {
    const answers = createInspectionAnswers()
    expect(Object.keys(answers)).toHaveLength(11)
    expect(Object.values(answers).every((answer) => answer === null)).toBe(true)
    expect(isInspectionComplete(inspectionItemsFor('Pre-Trip'), answers)).toBe(false)
  })

  it('only presents a new request to an available driver without an active trip', () => {
    expect(canReceiveRequest('Available', 'none')).toBe(true)
    expect(canReceiveRequest('On Break', 'none')).toBe(false)
    expect(canReceiveRequest('Available', 'active')).toBe(false)
  })

  it('uses distinct Figma checklists for pre-trip and post-trip', () => {
    expect(inspectionItemsFor('Pre-Trip')).toContain('Tyres & Wheel Condition')
    expect(inspectionItemsFor('Post-Trip')).toContain('Lost Passenger Belongings')
    expect(inspectionItemsFor('Post-Trip')).not.toContain('Tyres & Wheel Condition')
  })

  it('advances the accepted trip through the Figma slide-confirm stages', () => {
    expect(activeTripStages).toEqual([
      'En Route to Pickup',
      'Arrived at Pickup',
      'Passenger Boarded',
      'In Transit',
      'Arrived at Destination',
      'Completed',
    ])
    expect(nextTripStage(0)).toBe(1)
    expect(nextTripStage(5)).toBeNull()
  })

  it('maps backend stages and actions to the full journey progress', () => {
    expect(tripStageIndex('DRIVER_ASSIGNED')).toBe(-1)
    expect(tripStageIndex('DRIVER_EN_ROUTE')).toBe(0)
    expect(tripStageIndex('PASSENGER_ON_BOARD')).toBe(2)
    expect(tripStageIndex('COMPLETED')).toBe(5)
    expect(progressActionLabel('ARRIVE_AT_PICKUP')).toBe('Arrived at Pickup')
  })

  it('attaches each stage time node from the first matching trip event', () => {
    const times = stageTimesFor([
      { type: 'STARTED_EN_ROUTE', occurredAt: '2026-10-22T08:05:00Z' },
      { type: 'ARRIVED_AT_PICKUP', occurredAt: '2026-10-22T08:20:00Z' },
      { type: 'PASSENGER_BOARDED', occurredAt: '2026-10-22T08:30:00Z' },
      { type: 'DRIVER_EN_ROUTE', occurredAt: '2026-10-22T08:40:00Z' },
    ])
    expect(times).toEqual([
      '2026-10-22T08:05:00Z', '2026-10-22T08:20:00Z', '2026-10-22T08:30:00Z', null, null, null,
    ])
    expect(stageTimesFor([{ type: 'DRIVER_ASSIGNED', occurredAt: '2026-10-22T07:00:00Z' }])).toEqual(Array(activeTripStages.length).fill(null))
  })
})
