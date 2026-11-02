import { describe, expect, it } from 'vitest'
import homePage from './index.vue?raw'
import driverApi from '@/api/driver.ts?raw'

describe('driver home shifts and availability', () => {
  it('queries the driver’s shifts from the staff shift endpoint using the signed-in driver ID', () => {
    expect(driverApi).toContain('/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}/shifts')
    expect(driverApi).toContain("pageSize: 50, sort: '-businessDate,-shiftId'")
    expect(homePage).toContain("fetchUpcomingDriverShifts(driverId, macauBusinessDate(new Date()))")
  })

  it('loads the next seven days after identifying the signed-in driver', () => {
    expect(homePage).toContain('loadDriverShifts()')
    expect(homePage).toContain("{{ $t('Next 7 Days Shifts') }}")
    expect(homePage).toContain("{{ $t('No Shift') }}")
    expect(homePage).toContain('shiftError')
  })

  it('shows each active journey stage time node', () => {
    expect(homePage).toContain('stageTimesFor(activeTrip.value?.events')
    expect(homePage).toContain('journey-stage-time')
    expect(homePage).toContain('formatStageTime(activeStageTimes[index]!)')
  })

  it('renders availability as read-only status without a change handler', () => {
    expect(homePage).toContain('availability-segmented" :data-status="availability" role="status"')
    expect(homePage).not.toContain('changeAvailability(')
    expect(homePage).not.toContain('updateAvailability(')
  })
})
