# Platform API（主档与配置服务）镜像。构建上下文是 backend/（不是本目录），例如：
#   docker build -f src/Services/Platform/LMS.Platform.Api/Dockerfile -t lms-platform-api backend/
# 部署机上由 backend/docker/docker-compose.app.yml 统一构建启动，也可用仓库根目录的 ops.py。
#
# 运行阶段基于 alpine，装 tzdata 与 ICU：业务时区（Business:TimeZoneId = Asia/Macau）与本地化格式都需要它们。
# 监听端口沿用开发端口 5688，与 compose 里的 ASPNETCORE_URLS 保持一致。

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache tzdata icu-libs \
    && cp /usr/share/zoneinfo/Asia/Macau /etc/localtime \
    && echo "Asia/Macau" > /etc/timezone
ENV TZ=Asia/Macau \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=5688
EXPOSE 5688

# 发布阶段：整份 backend 源码进上下文，只发布本服务及其引用项目。
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS publish
WORKDIR /src
COPY . .
RUN dotnet publish "src/Services/Platform/LMS.Platform.Api/LMS.Platform.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LMS.Platform.Api.dll"]
