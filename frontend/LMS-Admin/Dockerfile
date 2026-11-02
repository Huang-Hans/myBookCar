FROM node:22-alpine AS build
WORKDIR /src
COPY LMS-Admin/package.json LMS-Admin/package-lock.json ./
RUN npm ci
# Keep sibling source directories under the installed dependencies.
COPY LMS-Admin/ ./LMS-Admin/
COPY shared/ ./shared/
WORKDIR /src/LMS-Admin
RUN npm run build

FROM nginx:alpine AS final
RUN apk add --no-cache tzdata \
    && cp /usr/share/zoneinfo/Asia/Macau /etc/localtime \
    && echo "Asia/Macau" > /etc/timezone
ENV TZ=Asia/Macau NGINX_ENTRYPOINT_QUIET_LOGS=1
COPY LMS-Admin/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/LMS-Admin/dist /usr/share/nginx/html
EXPOSE 5188
