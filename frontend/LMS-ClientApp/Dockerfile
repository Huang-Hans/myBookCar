FROM node:22-alpine AS build
WORKDIR /src
COPY LMS-ClientApp/package.json LMS-ClientApp/package-lock.json ./
RUN npm ci
COPY LMS-ClientApp/ ./

# 共享的地图配置在仓库 frontend 根目录：app 以 ../map.env 与 ../../../../map.config 引用它们，
# 因此构建上下文是 frontend/，这两个文件必须落到 /（也就是 /src 的上一级）。
COPY map.env.ts map.config.ts /
# The app also imports ../../../shared/tripLocations/*, so the shared sources must sit at /shared.
COPY shared/ /shared/

# AMap 的 key 是构建期变量：本机读 frontend/.env.local（map.env.ts 的 loadEnv），
# Docker 里由 compose build.args 传入，loadEnv 会合并 process.env。空值即页面显示 “AMap is not configured”。
ARG VITE_AMAP_WEB_KEY=
ARG VITE_AMAP_SECURITY_JS_CODE=
ENV VITE_AMAP_WEB_KEY=$VITE_AMAP_WEB_KEY
ENV VITE_AMAP_SECURITY_JS_CODE=$VITE_AMAP_SECURITY_JS_CODE

RUN npm run build:h5

FROM nginx:alpine AS final
RUN apk add --no-cache tzdata     && cp /usr/share/zoneinfo/Asia/Macau /etc/localtime     && echo "Asia/Macau" > /etc/timezone
ENV TZ=Asia/Macau NGINX_ENTRYPOINT_QUIET_LOGS=1
COPY LMS-ClientApp/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/build/h5 /usr/share/nginx/html
EXPOSE 5192
