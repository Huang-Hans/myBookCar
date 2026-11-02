FROM node:22-alpine AS build
WORKDIR /src
COPY LMS-User/package.json LMS-User/package-lock.json ./
RUN npm ci
# Keep sibling source directories under the installed dependencies.
COPY LMS-User/ ./LMS-User/
COPY shared/ ./shared/
WORKDIR /src/LMS-User
RUN npm run build

FROM nginx:alpine AS final
RUN apk add --no-cache tzdata \
    && cp /usr/share/zoneinfo/Asia/Macau /etc/localtime \
    && echo "Asia/Macau" > /etc/timezone
ENV TZ=Asia/Macau NGINX_ENTRYPOINT_QUIET_LOGS=1
COPY LMS-User/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/LMS-User/dist /usr/share/nginx/html
EXPOSE 5189
