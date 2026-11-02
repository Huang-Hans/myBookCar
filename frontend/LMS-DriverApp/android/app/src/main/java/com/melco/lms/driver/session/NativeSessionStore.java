package com.melco.lms.driver.session;

import android.content.Context;
import android.content.SharedPreferences;

import com.melco.lms.driver.net.JsonHttpClient;
import com.melco.lms.driver.net.JsonResponse;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

/**
 * Persistent driver session shared by the H5 page and the location service.
 * Refresh tokens rotate on every use and a reused one revokes the whole chain, so all refreshes
 * go through {@link #refresh} under one lock and always start from the newest stored token.
 */
public final class NativeSessionStore {
    private static final String PREFERENCES = "lms_driver_session";
    private static final String SESSION_KEY = "session";
    private static final String REFRESH_PATH = "/api/platform/driver/v1/token-refreshes";
    private static NativeSessionStore instance;

    private final SharedPreferences preferences;

    private NativeSessionStore(Context context) {
        preferences = context.getApplicationContext().getSharedPreferences(PREFERENCES, Context.MODE_PRIVATE);
    }

    public static synchronized NativeSessionStore get(Context context) {
        if (instance == null) instance = new NativeSessionStore(context);
        return instance;
    }

    public synchronized String sessionJson() {
        return preferences.getString(SESSION_KEY, "");
    }

    public synchronized void save(String sessionJson) {
        preferences.edit().putString(SESSION_KEY, sessionJson).apply();
    }

    public synchronized void clear() {
        preferences.edit().remove(SESSION_KEY).apply();
    }

    public synchronized String accessToken() {
        return field("accessToken");
    }

    public synchronized String refreshToken() {
        return field("refreshToken");
    }

    /**
     * Rotates the session unless someone already did: when the stored refresh token differs from the
     * caller's stale one, the newer session is returned without calling the gateway.
     */
    public synchronized SessionRefreshResult refresh(String gatewayOrigin, String staleRefreshToken) {
        String current = field("refreshToken");
        if (current.isEmpty()) {
            return SessionRefreshResult.failed(401, 40100, "Your session has expired.", "unavailable");
        }
        if (!current.equals(staleRefreshToken)) {
            return SessionRefreshResult.succeeded(sessionJson());
        }
        return requestRotation(gatewayOrigin, current);
    }

    private SessionRefreshResult requestRotation(String gatewayOrigin, String refreshToken) {
        try {
            JSONObject body = new JSONObject().put("refreshToken", refreshToken);
            JsonResponse response = JsonHttpClient.post(gatewayOrigin + REFRESH_PATH, body, null);
            JSONObject session = response.data();
            if (!response.isSuccess() || session == null) {
                return SessionRefreshResult.failed(response.statusCode, response.code(), response.message(), response.traceId());
            }
            save(session.toString());
            return SessionRefreshResult.succeeded(session.toString());
        } catch (IOException | JSONException failure) {
            return SessionRefreshResult.networkFailure();
        }
    }

    private String field(String name) {
        String json = sessionJson();
        if (json.isEmpty()) return "";
        try {
            return new JSONObject(json).optString(name, "");
        } catch (JSONException invalid) {
            return "";
        }
    }
}
