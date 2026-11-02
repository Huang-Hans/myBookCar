# LMS Admin Portal

Independent Vue 3 + TypeScript Portal Management System. The checked-in LMS gateway snapshot is [openapi/lms-all-services.json](openapi/lms-all-services.json).

```powershell
npm ci
npm run dev
npm run typecheck
npm run lint
npm run test
npm run build
```

Open [http://127.0.0.1:5173/portal/locations](http://127.0.0.1:5173/portal/locations). Normal mode requires a provisioned corporate account. All LMS API requests use same-origin `/api/`; Vite and Nginx forward them to `https://melco1.weprogram.site`. This avoids browser CORS errors while keeping the upstream gateway fixed. Set `VITE_LMS_DEMO=true` only for an API-free design preview. Tokens remain in memory or `sessionStorage`.

The project has its own Dockerfile and nginx configuration; the application Compose file deploys it as `admin-web` on port 5188. Production ingress must enforce HTTPS, HSTS, and the approved CORS policy.
