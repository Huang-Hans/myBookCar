import type { TripLocation } from '../../../shared/tripLocations/contracts'

const bySeq = (left: TripLocation, right: TripLocation): number => left.seq - right.seq

function lastSeqOf(points: readonly TripLocation[]): number {
  return points.reduce((maximum, point) => Math.max(maximum, point.seq), 0)
}

function uniqueBySeq(preferred: readonly TripLocation[], supplement: readonly TripLocation[]): TripLocation[] {
  const merged = new Map<number, TripLocation>()
  for (const point of preferred) merged.set(point.seq, point)
  for (const point of supplement) if (!merged.has(point.seq)) merged.set(point.seq, point)
  return [...merged.values()].sort(bySeq)
}

/**
 * Merges points fetched after `afterSeq` into the points already on screen.
 * A sampled response replaces everything it covers instead of interleaving with it.
 */
export function mergeTrackPoints(existing: readonly TripLocation[], incoming: readonly TripLocation[], sampled: boolean, afterSeq: number): TripLocation[] {
  if (!sampled || incoming.length === 0) return uniqueBySeq(existing, incoming)
  const coveredUntilSeq = lastSeqOf(incoming)
  const outsideCoverage = existing.filter((point) => point.seq <= afterSeq || point.seq > coveredUntilSeq)
  return uniqueBySeq(incoming, outsideCoverage)
}
