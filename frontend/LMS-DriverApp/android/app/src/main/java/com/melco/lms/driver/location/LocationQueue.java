package com.melco.lms.driver.location;

import android.content.Context;
import android.content.SharedPreferences;
import android.location.Location;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

/**
 * Pending points and the trip sequence, persisted so a recreated service resumes the same trip.
 * The sequence survives stop() so a later start for the same trip never reuses a number.
 */
public final class LocationQueue {
    private static final String PREFERENCES = "lms_trip_location";
    private static final String TRIP_ID = "tripId";
    private static final String LAST_SEQ = "lastSeq";
    private static final String GATEWAY_ORIGIN = "gatewayOrigin";
    private static final String IS_ACTIVE = "isActive";
    private static final String POINTS = "points";
    // About 1.7 hours of points at one per 3 seconds; older points are dropped first when offline longer.
    private static final int MAX_QUEUED_POINTS = 2000;
    private static LocationQueue instance;

    private final SharedPreferences preferences;
    private final List<LocationSample> points = new ArrayList<>();
    private String tripId;
    private long lastSeq;
    private String gatewayOrigin;
    private boolean isActive;

    private LocationQueue(Context context) {
        preferences = context.getApplicationContext().getSharedPreferences(PREFERENCES, Context.MODE_PRIVATE);
        tripId = preferences.getString(TRIP_ID, "");
        lastSeq = preferences.getLong(LAST_SEQ, 0);
        gatewayOrigin = preferences.getString(GATEWAY_ORIGIN, "");
        isActive = preferences.getBoolean(IS_ACTIVE, false);
        restorePoints(preferences.getString(POINTS, "[]"));
    }

    public static synchronized LocationQueue get(Context context) {
        if (instance == null) instance = new LocationQueue(context);
        return instance;
    }

    /** Starts or resumes a trip; the sequence continues from the larger of native and H5 values. */
    public synchronized void begin(String newTripId, long knownSeq, String newGatewayOrigin) {
        if (!newTripId.equals(tripId)) {
            tripId = newTripId;
            lastSeq = 0;
            points.clear();
        }
        lastSeq = Math.max(lastSeq, Math.max(0, knownSeq));
        gatewayOrigin = newGatewayOrigin;
        isActive = true;
        persist();
    }

    /** Ends reporting for the current trip and drops unsent points; the trip id and sequence are kept. */
    public synchronized void finish() {
        isActive = false;
        points.clear();
        persist();
    }

    /** Appends a fix with the next sequence number; invalid coordinates are skipped without using a number. */
    public synchronized void enqueue(Location fix) {
        LocationSample sample = LocationSample.fromFix(lastSeq + 1, fix);
        if (sample == null) return;
        lastSeq = sample.seq;
        points.add(sample);
        while (points.size() > MAX_QUEUED_POINTS) points.remove(0);
        persist();
    }

    public synchronized List<LocationSample> peek(int maxCount) {
        return new ArrayList<>(points.subList(0, Math.min(maxCount, points.size())));
    }

    /** Removes everything the server has stored (seq ≤ acceptedSeq). */
    public synchronized void acknowledge(long acceptedSeq) {
        lastSeq = Math.max(lastSeq, acceptedSeq);
        // Iterator removal instead of removeIf: minSdk is 23.
        Iterator<LocationSample> iterator = points.iterator();
        while (iterator.hasNext()) {
            if (iterator.next().seq <= acceptedSeq) iterator.remove();
        }
        persist();
    }

    /** Drops points the server rejected as invalid so they do not block the queue. */
    public synchronized void dropFirst(int count) {
        points.subList(0, Math.min(count, points.size())).clear();
        persist();
    }

    public synchronized String tripId() {
        return tripId;
    }

    public synchronized long lastSeq() {
        return lastSeq;
    }

    public synchronized String gatewayOrigin() {
        return gatewayOrigin;
    }

    public synchronized boolean isActive() {
        return isActive;
    }

    private void persist() {
        preferences.edit()
                .putString(TRIP_ID, tripId)
                .putLong(LAST_SEQ, lastSeq)
                .putString(GATEWAY_ORIGIN, gatewayOrigin)
                .putBoolean(IS_ACTIVE, isActive)
                .putString(POINTS, serializePoints())
                .apply();
    }

    private String serializePoints() {
        JSONArray array = new JSONArray();
        try {
            for (LocationSample point : points) array.put(point.toStorageJson());
        } catch (JSONException impossible) {
            return "[]";
        }
        return array.toString();
    }

    private void restorePoints(String json) {
        try {
            JSONArray array = new JSONArray(json);
            for (int index = 0; index < array.length(); index++) {
                points.add(LocationSample.fromJson(array.getJSONObject(index)));
            }
        } catch (JSONException corrupted) {
            points.clear();
        }
    }
}
