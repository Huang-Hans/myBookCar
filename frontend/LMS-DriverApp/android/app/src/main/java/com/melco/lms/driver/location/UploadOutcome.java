package com.melco.lms.driver.location;

/** What the service should do after one upload attempt (mirrors src/api/driverLocations.ts). */
public enum UploadOutcome {
    /** Server stored the batch; remove points up to acceptedSeq. */
    ACKNOWLEDGED,
    /** 40000 / 40001: drop this batch and continue. */
    REJECTED_BATCH,
    /** Network failure, 5xx or 429: keep the queue and retry later. */
    RETRY_LATER,
    /** 40400 / 40900: the trip no longer shares location; stop reporting. */
    TRIP_ENDED,
    /** 40100 or refresh failure: the driver must sign in again; stop reporting. */
    SESSION_EXPIRED
}
