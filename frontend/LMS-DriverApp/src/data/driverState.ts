export type InspectionAnswer = 'Pass' | 'Fail' | null
export type InspectionType = 'Pre-Trip' | 'Post-Trip'

const preTripItems = ['Tyres & Wheel Condition', 'Lights & Indicators', 'Brakes & Fluid Levels', 'Interior Cleanliness', 'Safety Equipment', 'Cross-Border Documents'] as const
const postTripItems = ['Exterior Damage', 'Interior Condition', 'Fuel Level', 'Lost Passenger Belongings', 'Vehicle Lock & Security'] as const

export function inspectionItemsFor(type: InspectionType): readonly string[] {
  return type === 'Post-Trip' ? postTripItems : preTripItems
}

export function createInspectionAnswers(): Record<string, InspectionAnswer> {
  return Object.fromEntries([...preTripItems, ...postTripItems].map((item) => [item, null]))
}

export function isInspectionComplete(items: readonly string[], answers: Record<string, InspectionAnswer>): boolean {
  return items.every((item) => answers[item] === 'Pass' || answers[item] === 'Fail')
}

export function canReceiveRequest(availability: string, tripStatus: string): boolean {
  return availability === 'Available' && tripStatus !== 'active'
}

export const activeTripStages = ['En Route to Pickup', 'Arrived at Pickup', 'Passenger Boarded', 'In Transit', 'Arrived at Destination', 'Completed'] as const

const tripStageIndexes: Readonly<Record<string, number>> = {
  DRIVER_EN_ROUTE: 0,
  EN_ROUTE_TO_PICKUP: 0,
  ARRIVED_AT_PICKUP: 1,
  PASSENGER_ON_BOARD: 2,
  IN_TRANSIT: 3,
  ARRIVED_AT_DESTINATION: 4,
  COMPLETED: 5,
}

export function tripStageIndex(stage: string): number {
  return tripStageIndexes[stage] ?? -1
}

const stageEventTypeIndexes: Readonly<Record<string, number>> = {
  STARTED_EN_ROUTE: 0,
  DRIVER_EN_ROUTE: 0,
  ARRIVED_AT_PICKUP: 1,
  PASSENGER_BOARDED: 2,
  IN_TRANSIT: 3,
  ARRIVED_AT_DESTINATION: 4,
  COMPLETED: 5,
}

// 各阶段的时间节点：取履约轨迹事件里每个阶段类型第一次出现的发生时间；未发生的阶段为 null。
export function stageTimesFor(events: ReadonlyArray<{ type?: string | null; occurredAt?: string | null }>): ReadonlyArray<string | null> {
  const times: (string | null)[] = Array(activeTripStages.length).fill(null)
  for (const event of events) {
    if (!event.occurredAt) continue
    const index = event.type ? stageEventTypeIndexes[event.type] : undefined
    if (index !== undefined && index >= 0 && !times[index]) times[index] = event.occurredAt
  }
  return times
}

export function progressActionLabel(action: string | undefined): string {
  const labels: Readonly<Record<string, string>> = {
    START_EN_ROUTE: 'En Route to Pickup',
    ARRIVE_AT_PICKUP: 'Arrived at Pickup',
    PICK_UP_PASSENGER: 'Passenger Boarded',
    START_TRANSIT: 'In Transit',
    ARRIVE_AT_DESTINATION: 'Arrived at Destination',
    COMPLETE_TRIP: 'Completed',
  }
  return action ? labels[action] ?? action.replace(/_/g, ' ') : ''
}

export function nextTripStage(index: number): number | null {
  return index >= activeTripStages.length - 1 ? null : Math.max(0, index + 1)
}
