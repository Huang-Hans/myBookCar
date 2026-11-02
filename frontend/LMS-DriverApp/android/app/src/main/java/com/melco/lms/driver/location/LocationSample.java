package com.melco.lms.driver.location;

import android.location.Location;

import org.json.JSONException;
import org.json.JSONObject;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/** One queued GCJ-02 point with its trip sequence number; field names match the upload contract. */
public final class LocationSample {
    public final long seq;
    public final double latitude;
    public final double longitude;
    public final Double accuracyMeters;
    public final Double headingDegrees;
    public final Double speedKmh;
    public final long recordedAtMillis;

    public LocationSample(long seq, double latitude, double longitude, Double accuracyMeters,
                          Double headingDegrees, Double speedKmh, long recordedAtMillis) {
        this.seq = seq;
        this.latitude = latitude;
        this.longitude = longitude;
        this.accuracyMeters = accuracyMeters;
        this.headingDegrees = headingDegrees;
        this.speedKmh = speedKmh;
        this.recordedAtMillis = recordedAtMillis;
    }

    /** Converts a system fix (WGS-84) into a GCJ-02 sample; returns null for invalid coordinates. */
    public static LocationSample fromFix(long seq, Location fix) {
        if (!isValidCoordinate(fix.getLatitude(), fix.getLongitude())) return null;
        double[] converted = CoordinateConverter.wgs84ToGcj02(fix.getLongitude(), fix.getLatitude());
        return new LocationSample(seq, converted[1], converted[0],
                fix.hasAccuracy() ? (double) fix.getAccuracy() : null,
                fix.hasBearing() ? (double) fix.getBearing() : null,
                fix.hasSpeed() ? fix.getSpeed() * 3.6 : null,
                fix.getTime() > 0 ? fix.getTime() : System.currentTimeMillis());
    }

    public static LocationSample fromJson(JSONObject json) {
        return new LocationSample(json.optLong("seq"), json.optDouble("latitude"), json.optDouble("longitude"),
                optional(json, "accuracyMeters"), optional(json, "headingDegrees"), optional(json, "speedKmh"),
                json.optLong("recordedAtMillis"));
    }

    /** Upload payload item (ISO 8601 UTC time with Z suffix). */
    public JSONObject toUploadJson() throws JSONException {
        JSONObject json = new JSONObject()
                .put("seq", seq)
                .put("latitude", latitude)
                .put("longitude", longitude)
                .put("recordedAt", isoUtc(recordedAtMillis));
        if (accuracyMeters != null && accuracyMeters >= 0) json.put("accuracyMeters", accuracyMeters);
        if (headingDegrees != null && headingDegrees >= 0 && headingDegrees <= 360) json.put("headingDegrees", headingDegrees);
        if (speedKmh != null && speedKmh >= 0) json.put("speedKmh", speedKmh);
        return json;
    }

    /** Persistence form; keeps the raw timestamp. */
    public JSONObject toStorageJson() throws JSONException {
        return toUploadJson().put("recordedAtMillis", recordedAtMillis);
    }

    private static boolean isValidCoordinate(double latitude, double longitude) {
        return !Double.isNaN(latitude) && !Double.isInfinite(latitude) && latitude >= -90 && latitude <= 90
                && !Double.isNaN(longitude) && !Double.isInfinite(longitude) && longitude >= -180 && longitude <= 180;
    }

    private static Double optional(JSONObject json, String name) {
        return json.has(name) ? json.optDouble(name) : null;
    }

    private static String isoUtc(long millis) {
        SimpleDateFormat format = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
        format.setTimeZone(TimeZone.getTimeZone("UTC"));
        return format.format(new Date(millis));
    }
}
