package com.melco.lms.driver.bridge;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.os.Build;

/**
 * Runtime permissions for trip reporting, requested one step at a time:
 * precise location → notifications (Android 13+) → "Allow all the time" (Android 10+).
 * Notifications and background location are optional: reporting already works with
 * while-in-use location, they only keep the notification visible and allow restarts in the background.
 */
public final class LocationPermissionFlow {
    public static final int REQUEST_LOCATION = 2001;
    public static final int REQUEST_NOTIFICATIONS = 2002;
    public static final int REQUEST_BACKGROUND_LOCATION = 2003;

    private final Activity activity;
    // Each permission is asked at most once per process so the driver is not prompted on every sync.
    private boolean hasAskedLocation;
    private boolean hasAskedNotifications;
    private boolean hasAskedBackgroundLocation;

    public LocationPermissionFlow(Activity activity) {
        this.activity = activity;
    }

    public boolean hasForegroundLocation() {
        return isGranted(Manifest.permission.ACCESS_FINE_LOCATION);
    }

    /** Requests the next missing permission, if any. Safe to call from any thread. */
    public void requestNextMissing() {
        activity.runOnUiThread(() -> {
            if (!hasForegroundLocation()) {
                // Asked once per process: a permanent denial returns at once and must not loop with the page.
                if (hasAskedLocation) return;
                hasAskedLocation = true;
                request(REQUEST_LOCATION, Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION);
            } else if (needsNotifications()) {
                hasAskedNotifications = true;
                request(REQUEST_NOTIFICATIONS, Manifest.permission.POST_NOTIFICATIONS);
            } else if (needsBackgroundLocation()) {
                hasAskedBackgroundLocation = true;
                // Android 11+ shows the settings page where the driver picks "Allow all the time".
                request(REQUEST_BACKGROUND_LOCATION, Manifest.permission.ACCESS_BACKGROUND_LOCATION);
            }
        });
    }

    public boolean isOwnRequest(int requestCode) {
        return requestCode == REQUEST_LOCATION || requestCode == REQUEST_NOTIFICATIONS
                || requestCode == REQUEST_BACKGROUND_LOCATION;
    }

    private boolean needsNotifications() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && !hasAskedNotifications
                && !isGranted(Manifest.permission.POST_NOTIFICATIONS);
    }

    private boolean needsBackgroundLocation() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q && !hasAskedBackgroundLocation
                && !isGranted(Manifest.permission.ACCESS_BACKGROUND_LOCATION);
    }

    private boolean isGranted(String permission) {
        return activity.checkSelfPermission(permission) == PackageManager.PERMISSION_GRANTED;
    }

    private void request(int requestCode, String... permissions) {
        activity.requestPermissions(permissions, requestCode);
    }
}
