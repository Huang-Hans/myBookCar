package com.melco.lms.driver.location;

import android.Manifest;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ServiceInfo;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.IBinder;
import android.os.Looper;
import android.os.SystemClock;

import com.melco.lms.driver.session.NativeSessionStore;

import java.util.List;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

/**
 * Foreground service (type location) that keeps reporting the active trip while the app is in the
 * background or the screen is off: system fix every ~3 s → GCJ-02 → queue → batch upload every 3 s.
 */
public final class TripLocationService extends Service {
    public static final String REASON_TRIP_ENDED = "TRIP_ENDED";
    public static final String REASON_SESSION_EXPIRED = "SESSION_EXPIRED";
    public static final String REASON_LOCATION_UNAVAILABLE = "LOCATION_UNAVAILABLE";
    private static final String ACTION_START = "com.melco.lms.driver.location.START";
    private static final String EXTRA_TRIP_ID = "tripId";
    private static final String EXTRA_LAST_SEQ = "lastSeq";
    private static final String EXTRA_GATEWAY_ORIGIN = "gatewayOrigin";
    private static final long INTERVAL_MS = 3_000;
    private static final int MAX_POINTS_PER_UPLOAD = 50;
    // A network fix only replaces a GPS fix once the GPS fix is this old.
    private static final long GPS_PREFERENCE_MS = 10_000;

    private static volatile boolean isRunning;
    private static volatile TripLocationStopListener stopListener;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private final LocationListener fixListener = new FixListener();
    private LocationQueue queue;
    private LocationUploader uploader;
    private LocationManager locationManager;
    private HandlerThread locationThread;
    private ScheduledExecutorService scheduler;
    private volatile Location latestFix;
    private long lastEnqueuedFixTime;
    private long resumeAtElapsed;

    public static boolean isRunning() {
        return isRunning;
    }

    public static void setStopListener(TripLocationStopListener listener) {
        stopListener = listener;
    }

    /** Must be called while the app is in the foreground (Android 12+ background-start limits). */
    public static void start(Context context, String tripId, long lastSeq, String gatewayOrigin) {
        Intent intent = new Intent(context, TripLocationService.class).setAction(ACTION_START)
                .putExtra(EXTRA_TRIP_ID, tripId)
                .putExtra(EXTRA_LAST_SEQ, lastSeq)
                .putExtra(EXTRA_GATEWAY_ORIGIN, gatewayOrigin);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) context.startForegroundService(intent);
        else context.startService(intent);
    }

    public static void stop(Context context) {
        LocationQueue.get(context).finish();
        context.stopService(new Intent(context, TripLocationService.class));
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null || !ACTION_START.equals(intent.getAction())) {
            stopSelf();
            return START_NOT_STICKY;
        }
        queue = LocationQueue.get(this);
        queue.begin(intent.getStringExtra(EXTRA_TRIP_ID), intent.getLongExtra(EXTRA_LAST_SEQ, 0),
                intent.getStringExtra(EXTRA_GATEWAY_ORIGIN));
        if (!enterForeground() || !startLocationUpdates()) {
            finishWith(REASON_LOCATION_UNAVAILABLE);
            return START_NOT_STICKY;
        }
        startUploadSchedule();
        isRunning = true;
        // Redeliver the start intent if the process is killed so the same trip resumes.
        return START_REDELIVER_INTENT;
    }

    @Override
    public void onDestroy() {
        isRunning = false;
        if (locationManager != null) locationManager.removeUpdates(fixListener);
        if (locationThread != null) locationThread.quitSafely();
        if (scheduler != null) scheduler.shutdownNow();
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private boolean enterForeground() {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                startForeground(TripLocationNotification.NOTIFICATION_ID, TripLocationNotification.build(this),
                        ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION);
            } else {
                startForeground(TripLocationNotification.NOTIFICATION_ID, TripLocationNotification.build(this));
            }
            return true;
        } catch (RuntimeException notAllowed) {
            // Missing location permission or a background start the system refused.
            return false;
        }
    }

    private boolean startLocationUpdates() {
        if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            return false;
        }
        // Switching trips while running keeps the existing listeners.
        if (locationThread != null) return true;
        locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        locationThread = new HandlerThread("lms-trip-location");
        locationThread.start();
        boolean hasProvider = requestUpdates(LocationManager.GPS_PROVIDER);
        hasProvider |= requestUpdates(LocationManager.NETWORK_PROVIDER);
        return hasProvider;
    }

    private boolean requestUpdates(String provider) {
        try {
            if (locationManager.getProvider(provider) == null) return false;
            locationManager.requestLocationUpdates(provider, INTERVAL_MS, 0, fixListener, locationThread.getLooper());
            return true;
        } catch (SecurityException | IllegalArgumentException unavailable) {
            return false;
        }
    }

    private void startUploadSchedule() {
        if (scheduler != null) return;
        scheduler = Executors.newSingleThreadScheduledExecutor();
        uploader = new LocationUploader(NativeSessionStore.get(this));
        // Fixed delay: a slow upload delays the next tick instead of overlapping it.
        scheduler.scheduleWithFixedDelay(this::tick, 0, INTERVAL_MS, TimeUnit.MILLISECONDS);
    }

    private void tick() {
        if (!queue.isActive() || SystemClock.elapsedRealtime() < resumeAtElapsed) return;
        try {
            enqueueLatestFix();
            uploadPending();
        } catch (RuntimeException unexpected) {
            // A scheduled task that throws is never run again; keep the schedule alive and retry next tick.
        }
    }

    // At most one point per interval even when GPS and network providers both report.
    private void enqueueLatestFix() {
        Location fix = latestFix;
        if (fix == null || fix.getTime() <= lastEnqueuedFixTime) return;
        lastEnqueuedFixTime = fix.getTime();
        queue.enqueue(fix);
    }

    private void uploadPending() {
        List<LocationSample> batch = queue.peek(MAX_POINTS_PER_UPLOAD);
        while (!batch.isEmpty() && handle(uploader.upload(queue.gatewayOrigin(), queue.tripId(), batch), batch)) {
            batch = queue.peek(MAX_POINTS_PER_UPLOAD);
        }
    }

    /** Applies one upload result; returns true when the next batch should be sent right away. */
    private boolean handle(UploadResult result, List<LocationSample> batch) {
        switch (result.outcome) {
            case ACKNOWLEDGED:
                queue.acknowledge(result.acceptedSeq);
                return result.acceptedSeq >= batch.get(0).seq;
            case REJECTED_BATCH:
                queue.dropFirst(batch.size());
                return true;
            case TRIP_ENDED:
                finishWith(REASON_TRIP_ENDED);
                return false;
            case SESSION_EXPIRED:
                finishWith(REASON_SESSION_EXPIRED);
                return false;
            default:
                deferUntilRetryAfter(result.retryAfterSeconds);
                return false;
        }
    }

    private void deferUntilRetryAfter(int retryAfterSeconds) {
        if (retryAfterSeconds <= 0) return;
        resumeAtElapsed = SystemClock.elapsedRealtime() + Math.min(5, retryAfterSeconds) * 1000L;
    }

    private void finishWith(String reason) {
        String tripId = queue.tripId();
        long lastSeq = queue.lastSeq();
        queue.finish();
        TripLocationStopListener listener = stopListener;
        if (listener != null) listener.onTripLocationStopped(tripId, lastSeq, reason);
        mainHandler.post(this::stopSelf);
    }

    private void onFix(Location fix) {
        Location current = latestFix;
        if (current == null || isPreferred(fix, current)) latestFix = fix;
    }

    private static boolean isPreferred(Location candidate, Location current) {
        if (candidate.getTime() < current.getTime()) return false;
        boolean isGps = LocationManager.GPS_PROVIDER.equals(candidate.getProvider());
        boolean isCurrentGps = LocationManager.GPS_PROVIDER.equals(current.getProvider());
        return isGps || !isCurrentGps || candidate.getTime() - current.getTime() > GPS_PREFERENCE_MS;
    }

    // All four callbacks are implemented: devices below API 30 have no default methods on LocationListener.
    private final class FixListener implements LocationListener {
        @Override
        public void onLocationChanged(Location location) {
            onFix(location);
        }

        @Override
        public void onStatusChanged(String provider, int status, Bundle extras) {
        }

        @Override
        public void onProviderEnabled(String provider) {
        }

        @Override
        public void onProviderDisabled(String provider) {
        }
    }
}
