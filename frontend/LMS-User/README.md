# LMS User Console

Independent Vue 3 + TypeScript Customer Service Web Console. The checked-in LMS gateway snapshot is [openapi/lms-all-services.json](openapi/lms-all-services.json).

```powershell
npm ci
npm run dev
npm run typecheck
npm run lint
npm run test
npm run build
```

Open [http://127.0.0.1:5175/console/tracking](http://127.0.0.1:5175/console/tracking). Port 5174 is occupied by the existing local `bookCar` process. Normal mode requires a provisioned corporate account. LMS API requests and SignalR notifications use same-origin `/api/`; Vite and Nginx forward them to `https://melco1.weprogram.site`, avoiding browser CORS errors. Set `VITE_LMS_DEMO=true` only for an API-free design preview. Tokens remain in memory or `sessionStorage`.

The project has its own Dockerfile and nginx configuration; the application Compose file deploys it as `user-web` on port 5189. Production ingress must enforce HTTPS, HSTS, and the approved CORS policy.

The dispatch order drawer loads on-duty driver choices from `GET /api/dispatch/staff/v1/shifts/board` for the trip's Macau business date and property. The upstream Swagger contract names its query parameters `Date` and `PropertyId`; this legacy casing is isolated in the API adapter as a WINSON-DEVIATION.
