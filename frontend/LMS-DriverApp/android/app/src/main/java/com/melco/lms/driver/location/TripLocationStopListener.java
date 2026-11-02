package com.melco.lms.driver.location;

/** Notified when the service stops on its own (trip ended or session expired) so the page can react. */
public interface TripLocationStopListener {
    /**
     * @param tripId  trip that stopped reporting
     * @param lastSeq last sequence number used for that trip
     * @param reason  TRIP_ENDED, SESSION_EXPIRED or LOCATION_UNAVAILABLE
     */
    void onTripLocationStopped(String tripId, long lastSeq, String reason);
}
