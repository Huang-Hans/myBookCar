package com.melco.lms.client;

import android.Manifest;
import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Insets;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.WindowInsets;
import android.widget.FrameLayout;
import android.widget.Toast;
import android.webkit.GeolocationPermissions;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import java.io.IOException;
import java.io.InputStream;

public class MainActivity extends Activity {
    private static final String ASSET_ORIGIN = "https://melco1.weprogram.site";
    private static final int LOCATION_REQUEST = 1001;
    private WebView webView;
    private String pendingOrigin;
    private GeolocationPermissions.Callback pendingLocationCallback;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(Color.WHITE);
        getWindow().setNavigationBarColor(Color.WHITE);
        getWindow().getDecorView().setSystemUiVisibility(
                getWindow().getDecorView().getSystemUiVisibility()
                        | View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR
                        | (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O
                        ? View.SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR : 0));

        webView = new WebView(this);
        FrameLayout root = new FrameLayout(this);
        root.setBackgroundColor(Color.WHITE);
        root.addView(webView, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT));
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            getWindow().setDecorFitsSystemWindows(false);
            root.setOnApplyWindowInsetsListener((view, windowInsets) -> {
                int handledTypes = WindowInsets.Type.systemBars()
                        | WindowInsets.Type.displayCutout()
                        | WindowInsets.Type.ime();
                Insets safeInsets = windowInsets.getInsets(handledTypes);
                view.setPadding(safeInsets.left, safeInsets.top,
                        safeInsets.right, safeInsets.bottom);
                return new WindowInsets.Builder(windowInsets)
                        .setInsets(handledTypes, Insets.NONE)
                        .build();
            });
        }
        setContentView(root);

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setGeolocationEnabled(true);
        settings.setAllowFileAccess(true);
        settings.setAllowContentAccess(true);
        settings.setMediaPlaybackRequiresUserGesture(false);

        webView.setWebViewClient(new LocalAssetClient());
        webView.setWebChromeClient(new WebChromeClient() {
            @Override
            public void onGeolocationPermissionsShowPrompt(
                    String origin,
                    GeolocationPermissions.Callback callback) {
                if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION)
                        == PackageManager.PERMISSION_GRANTED) {
                    callback.invoke(origin, true, false);
                    return;
                }
                pendingOrigin = origin;
                pendingLocationCallback = callback;
                requestPermissions(new String[] {
                        Manifest.permission.ACCESS_FINE_LOCATION,
                        Manifest.permission.ACCESS_COARSE_LOCATION
                }, LOCATION_REQUEST);
            }
        });

        webView.loadUrl(ASSET_ORIGIN + "/index.html");
    }

    @Override
    public void onRequestPermissionsResult(
            int requestCode,
            String[] permissions,
            int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == LOCATION_REQUEST && pendingLocationCallback != null) {
            boolean granted = grantResults.length > 0
                    && grantResults[0] == PackageManager.PERMISSION_GRANTED;
            pendingLocationCallback.invoke(pendingOrigin, granted, false);
            pendingOrigin = null;
            pendingLocationCallback = null;
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
        } else {
            super.onBackPressed();
        }
    }

    private final class LocalAssetClient extends WebViewClient {
        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            Uri uri = request.getUrl();
            if (!"tel".equalsIgnoreCase(uri.getScheme())) {
                return false;
            }

            String phoneNumber = uri.getSchemeSpecificPart();
            if (phoneNumber == null || !phoneNumber.matches("\\+[1-9][0-9]{7,14}")) {
                return true;
            }

            try {
                startActivity(new Intent(Intent.ACTION_DIAL, uri));
            } catch (ActivityNotFoundException | SecurityException exception) {
                Toast.makeText(MainActivity.this, "Phone dialer unavailable", Toast.LENGTH_SHORT).show();
            }
            return true;
        }

        @Override
        public WebResourceResponse shouldInterceptRequest(
                WebView view,
                WebResourceRequest request) {
            String path = request.getUrl().getPath();
            String host = request.getUrl().getHost();
            if (!"melco1.weprogram.site".equals(host) || path == null || path.contains("..")
                    || (!"/index.html".equals(path) && !path.startsWith("/assets/")
                    && !path.startsWith("/static/"))) {
                return super.shouldInterceptRequest(view, request);
            }

            try {
                InputStream stream = getAssets().open("www" + path);
                return new WebResourceResponse(mimeType(path), "UTF-8", stream);
            } catch (IOException ignored) {
                return super.shouldInterceptRequest(view, request);
            }
        }

        private String mimeType(String path) {
            if (path.endsWith(".html")) return "text/html";
            if (path.endsWith(".js")) return "application/javascript";
            if (path.endsWith(".css")) return "text/css";
            if (path.endsWith(".svg")) return "image/svg+xml";
            if (path.endsWith(".png")) return "image/png";
            if (path.endsWith(".jpg") || path.endsWith(".jpeg")) return "image/jpeg";
            if (path.endsWith(".webp")) return "image/webp";
            return "application/octet-stream";
        }
    }
}
