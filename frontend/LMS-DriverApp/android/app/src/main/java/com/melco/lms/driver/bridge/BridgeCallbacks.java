package com.melco.lms.driver.bridge;

import android.webkit.WebView;

import org.json.JSONObject;

import java.lang.ref.WeakReference;

/** Native → H5 delivery through evaluateJavascript; payloads are passed as quoted JSON strings. */
public final class BridgeCallbacks {
    private final WeakReference<WebView> webView;

    public BridgeCallbacks(WebView webView) {
        this.webView = new WeakReference<>(webView);
    }

    /** Resolves a pending H5 promise registered under callbackId. */
    public void deliver(String callbackId, String json) {
        run("window.__lmsDriverNativeCallback && window.__lmsDriverNativeCallback("
                + JSONObject.quote(callbackId) + "," + JSONObject.quote(json) + ")");
    }

    /** Broadcasts an event such as TRIP_LOCATION_STOPPED or LOCATION_PERMISSION_CHANGED. */
    public void emit(String json) {
        run("window.__lmsDriverNativeEvent && window.__lmsDriverNativeEvent(" + JSONObject.quote(json) + ")");
    }

    private void run(String script) {
        WebView view = webView.get();
        if (view == null) return;
        view.post(() -> view.evaluateJavascript(script, null));
    }
}
