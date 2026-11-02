package com.melco.lms.driver.bridge;

import android.app.Activity;
import android.content.Context;
import android.webkit.JavascriptInterface;

import com.melco.lms.driver.location.LocationQueue;
import com.melco.lms.driver.location.TripLocationService;
import com.melco.lms.driver.location.TripLocationStopListener;
import com.melco.lms.driver.session.NativeSessionStore;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Exposed to the bundled H5 page as window.LmsDriverNative. Hands trip reporting to
 * {@link TripLocationService} and keeps the session in {@link NativeSessionStore} so the page
 * and the service share one serialized refresh. Every call is ignored unless the page is trusted.
 */
public final class DriverNativeBridge implements TripLocationStopListener {
    public static final String NAME = "LmsDriverNative";

    private final Context context;
    private final BridgeCallbacks callbacks;
    private final LocationPermissionFlow permissions;
    private final NativeSessionStore sessions;
    private final ExecutorService refreshExecutor = Executors.newSingleThreadExecutor();
    private volatile boolean isTrustedPage;

    public DriverNativeBridge(Activity activity, BridgeCallbacks callbacks) {
        this.context = activity.getApplicationContext();
        this.callbacks = callbacks;
        this.permissions = new LocationPermissionFlow(activity);
        this.sessions = NativeSessionStore.get(activity);
        TripLocationService.setStopListener(this);
    }

    /** Updated by the WebViewClient on every navigation; only the bundled origin may use the bridge. */
    public void setTrustedPage(boolean isTrustedPage) {
        this.isTrustedPage = isTrustedPage;
    }

    public void onPermissionsResult(int requestCode) {
        if (!permissions.isOwnRequest(requestCode)) return;
        // Only a grant is worth a resync; the page then hands the trip to the native service.
        if (requestCode == LocationPermissionFlow.REQUEST_LOCATION && permissions.hasForegroundLocation()) {
            callbacks.emit("{\"type\":\"LOCATION_PERMISSION_CHANGED\"}");
        }
        permissions.requestNextMissing();
    }

    public void dispose() {
        TripLocationService.setStopListener(null);
        refreshExecutor.shutdownNow();
    }

    @JavascriptInterface
    public boolean isAvailable() {
        return isTrustedPage;
    }

    /** Returns false when location permission is still missing; the page then keeps its own reporter. */
    @JavascriptInterface
    public boolean startTripLocation(String tripId, double lastSeq, String gatewayOrigin) {
        if (!isTrustedPage || tripId == null || tripId.isEmpty()) return false;
        if (!permissions.hasForegroundLocation()) {
            permissions.requestNextMissing();
            return false;
        }
        TripLocationService.start(context, tripId, (long) lastSeq, gatewayOrigin);
        permissions.requestNextMissing();
        return true;
    }

    /** Stops reporting and returns the last sequence number used, for the page to store. */
    @JavascriptInterface
    public double stopTripLocation(String tripId) {
        if (!isTrustedPage) return 0;
        LocationQueue queue = LocationQueue.get(context);
        long lastSeq = tripId != null && tripId.equals(queue.tripId()) ? queue.lastSeq() : 0;
        TripLocationService.stop(context);
        return lastSeq;
    }

    @JavascriptInterface
    public String getTripLocationState() {
        if (!isTrustedPage) return "{}";
        LocationQueue queue = LocationQueue.get(context);
        try {
            return new JSONObject()
                    .put("tripId", queue.tripId())
                    .put("lastSeq", queue.lastSeq())
                    .put("isRunning", TripLocationService.isRunning() && queue.isActive())
                    // Active but not running: the process was killed and the service has not been restarted.
                    .put("isActive", queue.isActive())
                    .toString();
        } catch (JSONException impossible) {
            return "{}";
        }
    }

    @JavascriptInterface
    public String getSession() {
        return isTrustedPage ? sessions.sessionJson() : "";
    }

    @JavascriptInterface
    public void saveSession(String sessionJson) {
        if (isTrustedPage && sessionJson != null) sessions.save(sessionJson);
    }

    @JavascriptInterface
    public void clearSession() {
        if (isTrustedPage) sessions.clear();
    }

    /** Asynchronous: the result arrives through window.__lmsDriverNativeCallback(callbackId, json). */
    @JavascriptInterface
    public void refreshSession(String gatewayOrigin, String staleRefreshToken, String callbackId) {
        if (!isTrustedPage) return;
        refreshExecutor.execute(() -> callbacks.deliver(callbackId,
                sessions.refresh(gatewayOrigin, staleRefreshToken).toJson()));
    }

    @Override
    public void onTripLocationStopped(String tripId, long lastSeq, String reason) {
        try {
            callbacks.emit(new JSONObject()
                    .put("type", "TRIP_LOCATION_STOPPED")
                    .put("tripId", tripId)
                    .put("lastSeq", lastSeq)
                    .put("reason", reason)
                    .toString());
        } catch (JSONException impossible) {
            // The event is advisory; the page reconciles through getTripLocationState().
        }
    }
}
