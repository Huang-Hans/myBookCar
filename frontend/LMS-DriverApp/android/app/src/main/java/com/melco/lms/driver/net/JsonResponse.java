package com.melco.lms.driver.net;

import org.json.JSONObject;

/** Parsed gateway response: HTTP status, the unified envelope (may be null) and Retry-After seconds. */
public final class JsonResponse {
    private static final int FALLBACK_SERVER_ERROR = 50000;

    public final int statusCode;
    public final JSONObject envelope;
    public final int retryAfterSeconds;

    public JsonResponse(int statusCode, JSONObject envelope, int retryAfterSeconds) {
        this.statusCode = statusCode;
        this.envelope = envelope;
        this.retryAfterSeconds = retryAfterSeconds;
    }

    /** Application code from the envelope; falls back to a 5xx code when the body is not an envelope. */
    public int code() {
        if (envelope == null) return statusCode >= 200 && statusCode < 300 ? 20000 : FALLBACK_SERVER_ERROR;
        return envelope.optInt("code", FALLBACK_SERVER_ERROR);
    }

    public boolean isSuccess() {
        return statusCode >= 200 && statusCode < 300 && code() < 40000;
    }

    public JSONObject data() {
        return envelope == null ? null : envelope.optJSONObject("data");
    }

    public String message() {
        return envelope == null ? "" : envelope.optString("message", "");
    }

    public String traceId() {
        return envelope == null ? "unavailable" : envelope.optString("traceId", "unavailable");
    }
}
