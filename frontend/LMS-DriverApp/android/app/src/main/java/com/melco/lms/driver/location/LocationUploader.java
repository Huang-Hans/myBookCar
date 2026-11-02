package com.melco.lms.driver.location;

import com.melco.lms.driver.net.JsonHttpClient;
import com.melco.lms.driver.net.JsonResponse;
import com.melco.lms.driver.session.NativeSessionStore;
import com.melco.lms.driver.session.SessionRefreshResult;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.net.URLEncoder;
import java.util.List;

/** Posts one batch to the trip location API; refreshes the session once on 40101 through the shared store. */
public final class LocationUploader {
    private static final int TOKEN_EXPIRED = 40101;
    private final NativeSessionStore sessions;

    public LocationUploader(NativeSessionStore sessions) {
        this.sessions = sessions;
    }

    public UploadResult upload(String gatewayOrigin, String tripId, List<LocationSample> batch) {
        try {
            String url = gatewayOrigin + "/api/booking/driver/v1/me/trips/"
                    + URLEncoder.encode(tripId, "UTF-8") + "/locations";
            JSONObject body = requestBody(batch);
            JsonResponse response = JsonHttpClient.post(url, body, sessions.accessToken());
            if (response.code() == TOKEN_EXPIRED) {
                response = retryAfterRefresh(gatewayOrigin, url, body);
                if (response == null) return UploadResult.of(UploadOutcome.SESSION_EXPIRED);
            }
            return classify(response);
        } catch (IOException | JSONException failure) {
            return UploadResult.retryLater(0);
        }
    }

    private JsonResponse retryAfterRefresh(String gatewayOrigin, String url, JSONObject body) throws IOException {
        String staleRefreshToken = sessions.refreshToken();
        SessionRefreshResult refreshed = sessions.refresh(gatewayOrigin, staleRefreshToken);
        if (refreshed.isNetworkFailure) throw new IOException("Session refresh failed.");
        if (!refreshed.isSuccess) return null;
        return JsonHttpClient.post(url, body, sessions.accessToken());
    }

    private static JSONObject requestBody(List<LocationSample> batch) throws JSONException {
        JSONArray points = new JSONArray();
        for (LocationSample sample : batch) points.put(sample.toUploadJson());
        return new JSONObject().put("coordinateSystem", "GCJ02").put("points", points);
    }

    private static UploadResult classify(JsonResponse response) {
        if (response.isSuccess()) {
            JSONObject data = response.data();
            long acceptedSeq = data == null ? -1 : data.optLong("acceptedSeq", -1);
            return acceptedSeq >= 0 ? UploadResult.acknowledged(acceptedSeq) : UploadResult.retryLater(0);
        }
        switch (response.code()) {
            case 40000:
            case 40001:
                return UploadResult.of(UploadOutcome.REJECTED_BATCH);
            case 40100:
            case 40101:
                return UploadResult.of(UploadOutcome.SESSION_EXPIRED);
            case 40400:
            case 40900:
                return UploadResult.of(UploadOutcome.TRIP_ENDED);
            default:
                return UploadResult.retryLater(response.retryAfterSeconds);
        }
    }
}
