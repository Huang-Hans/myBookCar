package com.melco.lms.driver.net;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/** Minimal JSON POST client for the gateway (no third-party dependencies). */
public final class JsonHttpClient {
    private static final int CONNECT_TIMEOUT_MS = 10_000;
    private static final int READ_TIMEOUT_MS = 15_000;

    private JsonHttpClient() {
    }

    /** Sends a JSON POST; network failures surface as IOException so callers can keep data for retry. */
    public static JsonResponse post(String url, JSONObject body, String bearerToken) throws IOException {
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        try {
            connection.setRequestMethod("POST");
            connection.setConnectTimeout(CONNECT_TIMEOUT_MS);
            connection.setReadTimeout(READ_TIMEOUT_MS);
            connection.setDoOutput(true);
            connection.setRequestProperty("Accept", "application/json");
            connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
            connection.setRequestProperty("X-Trace-Id", "tr_native_" + Long.toString(System.nanoTime(), 36));
            if (bearerToken != null && !bearerToken.isEmpty()) {
                connection.setRequestProperty("Authorization", "Bearer " + bearerToken);
            }
            writeBody(connection, body);
            int statusCode = connection.getResponseCode();
            return new JsonResponse(statusCode, readEnvelope(connection, statusCode), retryAfter(connection));
        } finally {
            connection.disconnect();
        }
    }

    private static void writeBody(HttpURLConnection connection, JSONObject body) throws IOException {
        byte[] bytes = body.toString().getBytes(StandardCharsets.UTF_8);
        try (OutputStream output = connection.getOutputStream()) {
            output.write(bytes);
        }
    }

    private static JSONObject readEnvelope(HttpURLConnection connection, int statusCode) throws IOException {
        InputStream stream = statusCode >= 400 ? connection.getErrorStream() : connection.getInputStream();
        if (stream == null) return null;
        try (InputStream input = stream) {
            String text = new String(readAll(input), StandardCharsets.UTF_8);
            return text.isEmpty() ? null : new JSONObject(text);
        } catch (JSONException invalidBody) {
            return null;
        }
    }

    private static byte[] readAll(InputStream input) throws IOException {
        ByteArrayOutputStream buffer = new ByteArrayOutputStream();
        byte[] chunk = new byte[4096];
        int read;
        while ((read = input.read(chunk)) != -1) {
            buffer.write(chunk, 0, read);
        }
        return buffer.toByteArray();
    }

    private static int retryAfter(HttpURLConnection connection) {
        try {
            return Integer.parseInt(connection.getHeaderField("Retry-After"));
        } catch (NumberFormatException | NullPointerException missing) {
            return 0;
        }
    }
}
