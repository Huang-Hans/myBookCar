# LMS Client App

Vue 3 + TypeScript UniApp project copied from the local `bookCar` scaffold and restyled around the [client Figma reference](https://frost-wagon-31144857.figma.site/).

The app consumes the customer contracts in `docs/api/openapi/lms-customer.v1.yaml`: phone authentication and token rotation, service/location catalogs, ride options, trip creation and cancellation, account profile, favorites, preferences, and reviews. Registration and social identity remain unavailable because the current contract does not define those flows.

The bottom navigation exposes an Alerts entry on every signed-in page; the full-screen driver map has a floating entry. It shows the current active trip and up to 20 live trip updates received during this app session. The numeric badge counts unread live updates; new updates also trigger an arrival prompt. H5 caches only the active trip ID in `sessionStorage` so the current-trip banner remains visible when navigation reloads a page, then refreshes the trip from the API. The customer API has no notification-history or read-status endpoint, so the recent-update list and its read state are local to the session and are cleared on sign-out.

Trip details and the full-screen driver map reuse the customer notification SignalR connection. They invoke `WatchTrip` while visible, render the latest GCJ-02 driver position from the snapshot and `tripLocationUpdated` messages, re-watch after reconnect, and invoke `UnwatchTrip` when hidden. No device geolocation is presented as the driver's position. The customer app does not call the staff-only `/trips/{tripId}/track` endpoint from the track extension contract; its live-location contract is `docs/api-delivery/LMS-spec-20261018-02-trip-live-location-frontend.md`. Without a driver GPS point, the map keeps the pickup/drop-off route and shows a waiting state.

The full-screen driver map centers on the first reported GPS point and follows later points until the user drags the map. A route/driver control switches views; while focused on the driver, a route overview keeps the pickup-to-drop-off path visible. A reported location more than 50 km from both stops is not placed falsely on the route; the map shows its actual coordinates and warns that the driver GPS is outside the trip area.

H5 development listens on all network interfaces. H5 LMS API requests and SignalR notifications use same-origin `/api`; Vite and Nginx forward them to `https://melco1.weprogram.site` to avoid browser CORS errors. Native builds call that HTTPS gateway directly. Copy `.env.example` to `.env.local` only for local settings such as `VITE_DEV_SERVER_HOST`. Web tokens use `sessionStorage`; non-web UniApp targets use platform storage. A protected deep link redirects to sign-in and returns to the original page after successful authentication.

All client and driver route maps use the same AMap configuration. Copy `../.env.example` to `../.env.local`, then set `VITE_AMAP_WEB_KEY` and, if required by your AMap account, `VITE_AMAP_SECURITY_JS_CODE` there. Change that one file and restart both development servers or rebuild both apps. Keep the key browser-restricted and never commit `../.env.local`.

For Docker deployment, set `AMAP_WEB_KEY` in the untracked `backend/docker/.env` on the deployment host and rebuild the client image (`python3 ops.py client deploy`). Restarting an existing image cannot change its build-time map key. When the key is absent but both catalog locations have coordinates, H5 displays an approximate pickup-to-drop-off line and any reported driver position; geocoding and a road-following route still require the AMap key.

```powershell
npm ci
npm run dev:h5
npm run test
npm run typecheck
npm run build:h5
```

Native app builds require a provisioned SDK and signing profile. Production releases must use HTTPS and an OS-backed secure credential store.
