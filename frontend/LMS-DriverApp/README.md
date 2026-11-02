# LMS Driver App

Vue 3 + TypeScript UniApp project copied from the local `bookCar` scaffold and rebuilt around the [driver Figma reference](https://city-ideate-98859534.figma.site/).

The app consumes the driver contracts in `docs/api/openapi/lms-driver.v1.yaml`: phone authentication and token rotation, driver profile and availability, offers, active/history trips, fulfilment progress, inspections, vehicle issues, preferences, operations contacts, and incidents. GPS, live mileage, speed, ETA, and offline synchronization remain unavailable because the current contract does not define them.

H5 development listens on all network interfaces. H5 LMS API requests and SignalR notifications use same-origin `/api`; Vite and Nginx forward them to `https://melco1.weprogram.site` to avoid browser CORS errors. Native builds call that HTTPS gateway directly. Copy `.env.example` to `.env.local` only for local settings such as `VITE_DEV_SERVER_HOST`. Web tokens use `sessionStorage`; non-web UniApp targets use platform storage.

All driver and client route maps use the same AMap configuration. Copy `../.env.example` to `../.env.local`, then set `VITE_AMAP_WEB_KEY` and, if required, `VITE_AMAP_SECURITY_JS_CODE` there. Change that one file and restart both development servers or rebuild both apps. Keep the key browser-restricted and never commit `../.env.local`.

```powershell
npm ci
npm run dev:h5
npm run test
npm run typecheck
npm run build:h5
```

Native app builds require a provisioned SDK and signing profile. Production releases must use HTTPS and an OS-backed secure credential store.
