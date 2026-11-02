# LMS UserApp

Melco staff mobile workspace for quick guest bookings, booking lists, trip tracking, driver dispatch, and console notifications. Built with Vue 3, TypeScript, and Uniapp, following LMS-ClientApp and LMS-DriverApp.

## Run

```bash
cd frontend/LMS-UserApp
Copy-Item .env.example .env.local
npm ci
npm run dev:h5
```

H5 defaults to port **5193**. H5 LMS API requests use same-origin `/api`; Vite forwards them to `https://melco1.weprogram.site` to avoid browser CORS errors. The included Dockerfile and nginx configuration provide the same `/api` reverse proxy for production H5 hosting. Native builds call that HTTPS gateway directly. Authentication uses the existing staff `/api/v1/login` and `/api/v1/refresh` endpoints; no demo account or fabricated operational data is included.

## Verify

```bash
npm run typecheck
npm test
npm run build:h5
```

## Android debug APK

With Android SDK, Java 17, and Gradle installed, run `npm run build:android-apk`.
The installable debug package is written to `android/app/build/outputs/apk/debug/app-debug.apk`.
This is a WebView-based development package, not a signed production release.

Tracking, dispatch, and notifications refresh every 30 seconds while visible. The app consumes existing documented staff endpoints and does not add backend endpoints. Booking previews are required before creation; driver offers use the dispatch order version and an idempotency key. The API client uses the standard JSON envelope, bearer authentication, trace IDs, one token-refresh retry, and `Retry-After` on rate limits.

The trip detail map uses the existing staff location detail and trip track endpoints. It marks pickup and drop-off, draws only recorded GCJ-02 track segments, and displays the driver marker only while location sharing is active. The track refreshes every 3 seconds while the detail page is visible; transient failures retain the last route, while authorization/not-found responses hide it. AMap reads the shared, untracked `frontend/.env.local` browser key; copy `frontend/.env.example` and configure the AMap key before building. The original track frontend contract scoped its first rollout to LMS-User; this UserApp integration is an explicit product extension using the same existing API, not a new endpoint.

The dispatch driver sheet reads `GET /api/dispatch/staff/v1/shifts/board` for the order's Macau business date and property, then shows unique `ON_DUTY` drivers. The upstream OpenAPI names the two query parameters `Date` and `PropertyId`; these non-camelCase names are isolated in the API adapter as a documented Winson standard deviation. Driver invitations still use the dispatch offer endpoint, which performs final eligibility checks.

## Manual smoke

With the API running, sign in with a staff account with booking and dispatch permissions, then:

```bash
curl -i "$API_BASE/api/booking/staff/v1/trips?page=1&pageSize=20" -H "Authorization: Bearer $TOKEN" -H "Accept: application/json"
curl -i "$API_BASE/api/booking/staff/v1/console-notifications?page=1&pageSize=50" -H "Authorization: Bearer invalid" -H "Accept: application/json"
curl -i "$API_BASE/api/booking/staff/v1/trips/$TRIP_ID/track" -H "Authorization: Bearer $TOKEN" -H "Accept: application/json"
curl -i "$API_BASE/api/booking/staff/v1/trips/$TRIP_ID/track" -H "Authorization: Bearer invalid" -H "Accept: application/json"
```

Expect an HTTP 200 success envelope with `traceId` in the first case and an HTTP 401 error envelope with `traceId` in the second. Browser-test the quick booking preview and creation, status refresh, dispatch invitation, and mark-as-read interactions using a permitted account.

