# 网关镜像。构建上下文是 backend/（不是本目录），例如：
#   docker build -f src/LMS.ApiGateway/Dockerfile -t lms-api-gateway backend/
# 部署机上由 backend/docker/docker-compose.app.yml 统一构建启动，也可用仓库根目录的 ops.py。
#
# 运行阶段基于 alpine，装 tzdata 与 ICU：日期/数字的本地化格式依赖它们，缺失时进程会以固定区域性启动。
# 监听端口沿用开发端口 5088，与 compose 里的 ASPNETCORE_URLS 保持一致。

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache tzdata icu-libs \
    && cp /usr/share/zoneinfo/Asia/Macau /etc/localtime \
    && echo "Asia/Macau" > /etc/timezone
ENV TZ=Asia/Macau \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=5088
EXPOSE 5088

# 发布阶段：整份 backend 源码进上下文，只发布本服务及其引用项目。
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS publish
WORKDIR /src
COPY . .
RUN dotnet publish "src/LMS.ApiGateway/LMS.ApiGateway.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LMS.ApiGateway.dll"]
