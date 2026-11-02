// 与司机端 driverState.ts 的 activeTripStages 保持一致：
// 行程详情状态跟踪使用同一套行程进度描述，按固定阶段推进，避免时间线里同一状态重复出现。
export const customerTripStages = ['En Route to Pickup', 'Arrived at Pickup', 'Passenger Boarded', 'In Transit', 'Arrived at Destination', 'Completed'] as const

const tripStageIndexes: Readonly<Record<string, number>> = {
  DRIVER_EN_ROUTE: 0,
  EN_ROUTE_TO_PICKUP: 0,
  ARRIVED_AT_PICKUP: 1,
  PASSENGER_ON_BOARD: 2,
  IN_TRANSIT: 3,
  ARRIVED_AT_DESTINATION: 4,
  COMPLETED: 5,
}

// 当前行程阶段对应的进度下标；未开始/未知阶段返回 -1（所有阶段均为待进行）。
export function customerTripStageIndex(stage: string): number {
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

// 各阶段的时间节点：取轨迹事件里每个阶段类型第一次出现的发生时间；未发生的阶段为 null。
export function customerStageTimes(events: ReadonlyArray<{ type?: string | null; occurredAt: string }>): ReadonlyArray<string | null> {
  const times: (string | null)[] = Array(customerTripStages.length).fill(null)
  for (const event of events) {
    const index = event.type ? stageEventTypeIndexes[event.type] : undefined
    if (index !== undefined && index >= 0 && !times[index]) times[index] = event.occurredAt
  }
  return times
}
