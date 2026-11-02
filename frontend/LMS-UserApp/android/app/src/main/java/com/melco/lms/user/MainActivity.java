package com.melco.lms.user;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Insets;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.WindowInsets;
import android.widget.FrameLayout;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import java.io.IOException;
import java.io.InputStream;

public class MainActivity extends Activity {
    private static final String ASSET_ORIGIN = "https://melco1.weprogram.site";
    private WebView webView;

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
        settings.setCacheMode(WebSettings.LOAD_NO_CACHE);
        settings.setAllowFileAccess(true);
        settings.setAllowContentAccess(true);
        settings.setMediaPlaybackRequiresUserGesture(false);

        webView.setWebViewClient(new LocalAssetClient());
        webView.clearCache(true);
        webView.loadUrl(ASSET_ORIGIN + "/index.html");
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
