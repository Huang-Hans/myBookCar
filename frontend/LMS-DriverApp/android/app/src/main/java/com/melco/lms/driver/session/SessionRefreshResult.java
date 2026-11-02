package com.melco.lms.driver.session;

import org.json.JSONException;
import org.json.JSONObject;

/** Outcome of a serialized refresh: the current session JSON, or the gateway failure that ended it. */
public final class SessionRefreshResult {
    public final boolean isSuccess;
    public final boolean isNetworkFailure;
    public final String sessionJson;
    public final int statusCode;
    public final int code;
    public final String message;
    public final String traceId;

    private SessionRefreshResult(boolean isSuccess, boolean isNetworkFailure, String sessionJson,
                                 int statusCode, int code, String message, String traceId) {
        this.isSuccess = isSuccess;
        this.isNetworkFailure = isNetworkFailure;
        this.sessionJson = sessionJson;
        this.statusCode = statusCode;
        this.code = code;
        this.message = message;
        this.traceId = traceId;
    }

    public static SessionRefreshResult succeeded(String sessionJson) {
        return new SessionRefreshResult(true, false, sessionJson, 200, 20000, "success", "unavailable");
    }

    public static SessionRefreshResult failed(int statusCode, int code, String message, String traceId) {
        return new SessionRefreshResult(false, false, null, statusCode, code, message, traceId);
    }

    public static SessionRefreshResult networkFailure() {
        return new SessionRefreshResult(false, true, null, 0, 50000, "Network request failed.", "unavailable");
    }

    /** True when the session can no longer be refreshed and the driver must sign in again. */
    public boolean isSessionExpired() {
        return !isSuccess && !isNetworkFailure && code >= 40000 && code < 50000 && code != 42900;
    }

    /** JSON handed back to the H5 bridge callback. */
    public String toJson() {
        try {
            JSONObject json = new JSONObject()
                    .put("ok", isSuccess)
                    .put("statusCode", statusCode)
                    .put("code", code)
                    .put("message", message)
                    .put("traceId", traceId);
            if (sessionJson != null) json.put("session", new JSONObject(sessionJson));
            return json.toString();
        } catch (JSONException impossible) {
            return "{\"ok\":false,\"statusCode\":0,\"code\":50000}";
        }
    }
}
