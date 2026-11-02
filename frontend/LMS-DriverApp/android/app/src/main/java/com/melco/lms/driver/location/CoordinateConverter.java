package com.melco.lms.driver.location;

/**
 * WGS-84 (system GPS) to GCJ-02 (AMap, required by the upload API).
 * Mirrors wgs84ToGcj02 in src/data/amap.ts line by line so H5 and native points agree.
 */
public final class CoordinateConverter {
    private static final double GCJ_A = 6378245.0;
    private static final double GCJ_EE = 0.00669342162296594323;

    private CoordinateConverter() {
    }

    /** Returns {longitude, latitude} in GCJ-02. */
    public static double[] wgs84ToGcj02(double longitude, double latitude) {
        if (isOutsideChina(longitude, latitude)) return new double[] {longitude, latitude};
        double deltaLatitude = transformLatitude(longitude - 105.0, latitude - 35.0);
        double deltaLongitude = transformLongitude(longitude - 105.0, latitude - 35.0);
        double radianLatitude = latitude / 180.0 * Math.PI;
        double magic = Math.sin(radianLatitude);
        magic = 1 - GCJ_EE * magic * magic;
        double sqrtMagic = Math.sqrt(magic);
        deltaLatitude = deltaLatitude * 180.0 / ((GCJ_A * (1 - GCJ_EE)) / (magic * sqrtMagic) * Math.PI);
        deltaLongitude = deltaLongitude * 180.0 / (GCJ_A / sqrtMagic * Math.cos(radianLatitude) * Math.PI);
        return new double[] {longitude + deltaLongitude, latitude + deltaLatitude};
    }

    private static boolean isOutsideChina(double longitude, double latitude) {
        return longitude < 72.004 || longitude > 137.8347 || latitude < 0.8293 || latitude > 55.8271;
    }

    private static double transformLatitude(double longitude, double latitude) {
        double value = -100.0 + 2.0 * longitude + 3.0 * latitude + 0.2 * latitude * latitude
                + 0.1 * longitude * latitude + 0.2 * Math.sqrt(Math.abs(longitude));
        value += (20.0 * Math.sin(6.0 * longitude * Math.PI) + 20.0 * Math.sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0;
        value += (20.0 * Math.sin(latitude * Math.PI) + 40.0 * Math.sin(latitude / 3.0 * Math.PI)) * 2.0 / 3.0;
        value += (160.0 * Math.sin(latitude / 12.0 * Math.PI) + 320 * Math.sin(latitude * Math.PI / 30.0)) * 2.0 / 3.0;
        return value;
    }

    private static double transformLongitude(double longitude, double latitude) {
        double value = 300.0 + longitude + 2.0 * latitude + 0.1 * longitude * longitude
                + 0.1 * longitude * latitude + 0.1 * Math.sqrt(Math.abs(longitude));
        value += (20.0 * Math.sin(6.0 * longitude * Math.PI) + 20.0 * Math.sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0;
        value += (20.0 * Math.sin(longitude * Math.PI) + 40.0 * Math.sin(longitude / 3.0 * Math.PI)) * 2.0 / 3.0;
        value += (150.0 * Math.sin(longitude / 12.0 * Math.PI) + 300.0 * Math.sin(longitude / 30.0 * Math.PI)) * 2.0 / 3.0;
        return value;
    }
}
