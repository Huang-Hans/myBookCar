# LMS 行程实时定位前端契约

创建日期：2026-10-18  
状态：后端已实施（未做运行验证），前端按本契约对接。  
后端设计：[LMS-design-20261018-01-trip-live-location-backend.md](../design/LMS-design-20261018-01-trip-live-location-backend.md)  
现有实时通知契约：[LMS-spec-20261013-06-notification-signalr-frontend.md](LMS-spec-20261013-06-notification-signalr-frontend.md)

## 1. 适用范围

| 端 | 角色 | 用到的部分 |
|---|---|---|
| 司机端 LMS-DriverApp | 上报位置 | §2 |
| 客户端 LMS-ClientApp | 查看自己的行程 | §3、§4 |
| 客服网页控制台 LMS-User | 查看物业内行程 | §3、§4 |
| 客服移动端 LMS-UserApp | 第一期不接入 | — |

通用约定：字段使用小驼峰，时间为 ISO 8601 UTC（以 `Z` 结尾），坐标系统一为 **GCJ-02**。

## 2. 司机端：上报位置（HTTP）

```
POST /api/booking/driver/v1/me/trips/{tripId}/locations
Authorization: Bearer <司机令牌>
Content-Type: application/json
```

### 2.1 请求

```json
{
  "coordinateSystem": "GCJ02",
  "points": [
    {
      "seq": 1287,
      "latitude": 22.198745,
      "longitude": 113.543873,
      "accuracyMeters": 8.5,
      "headingDegrees": 132,
      "speedKmh": 36.4,
      "recordedAt": "2026-10-19T01:12:05.120Z"
    }
  ]
}
```

| 字段 | 必填 | 说明 |
|---|---|---|
| `coordinateSystem` | 是 | 固定为 `GCJ02`。设备原始坐标（通常是 WGS-84）需要先转换再上报。 |
| `points` | 是 | 1–50 个点。前台通常每次 1 个。 |
| `points[].seq` | 是 | 同一行程内单调递增的整数，从 1 开始。App 重启后从本地保存的值继续递增，不要重置。 |
| `points[].latitude` | 是 | −90～90 |
| `points[].longitude` | 是 | −180～180 |
| `points[].accuracyMeters` | 否 | 设备给出的精度，单位米 |
| `points[].headingDegrees` | 否 | 0–360，正北为 0 |
| `points[].speedKmh` | 否 | ≥ 0 |
| `points[].recordedAt` | 是 | 手机定位的时间，不是发送时间 |

### 2.2 响应

成功（200）：

```json
{ "code": 20000, "message": "success", "data": { "acceptedSeq": 1287 }, "traceId": "..." }
```

`acceptedSeq` 是服务端已保存的最大序号。重复发送的旧点不会报错，`acceptedSeq` 保持不变。

| HTTP / code | 情况 | 司机端处理 |
|---|---|---|
| 400 / 40001 | 字段不合法，整批未保存 | 丢弃这一批，记录日志 |
| 401 / 40101 | 令牌过期 | 走现有刷新令牌流程后重试 |
| 404 / 40400 | 不是本人的行程或不存在 | 停止上报 |
| 409 / 40900 | 不在共享窗口（尚未出发，或已完成 / 取消） | 停止上报 |
| 5xx / 网络失败 | 暂时不可用 | 保留这些点，下次与新点一起发送 |

### 2.3 上报时机

- 开始：司机执行"出发"（`START_EN_ROUTE`）成功之后。
- 频率：LMS-DriverApp 按产品要求在前台每 3 秒采集并上报一次（原契约为 5 秒，本次前端实施按明确需求调整）；位置没有变化也照常上报。GPS 获取超时或网络请求未完成时不会并发堆积，下一次成功采集后继续上报。
- 结束：执行"完成"成功，或收到 409 时停止。
- 第一期只要求前台上报。锁屏或切到后台后无法定位，属于已知限制。

## 3. 客户端 / 客服控制台：订阅行程位置

复用现有通知连接，**不新开连接**：

| 端 | 连接路径（不变） |
|---|---|
| 客户端 | `/api/booking/customer/v1/notifications` |
| 客服控制台 | `/api/booking/staff/v1/notifications` |

### 3.1 WatchTrip

```ts
const result = await connection.invoke('WatchTrip', tripId)
```

成功返回：

```json
{
  "code": 20000,
  "message": "success",
  "data": {
    "tripId": "trip_0190a1b2c3d4",
    "isSharing": true,
    "location": {
      "latitude": 22.198745,
      "longitude": 113.543873,
      "headingDegrees": 132,
      "speedKmh": 36.4,
      "seq": 1287,
      "recordedAt": "2026-10-19T01:12:05.120Z"
    }
  },
  "meta": { "traceId": "..." },
  "traceId": "..."
}
```

| 字段 | 说明 |
|---|---|
| `isSharing` | 当前是否处于共享窗口（司机已出发且行程未结束） |
| `location` | 最新位置；`isSharing` 为 `false`，或已出发但还没有收到上报时，为 `null` |
| `location.headingDegrees` / `speedKmh` | 司机端没上报时为 `null` |

失败时返回统一错误外壳，不抛出连接异常：

| code | 情况 | 前端处理 |
|---|---|---|
| 40001 | tripId 为空 | 检查调用参数 |
| 40100 | 连接未完成认证 | 走现有重连流程后再次 Watch |
| 40101 | 连接令牌过期 | 走现有重连流程后再次 Watch |
| 40300 | 客服当前权限不足（仅客服端） | 不显示车辆位置 |
| 40400 | 行程不存在或无权查看 | 不显示车辆位置 |
| 50300 | 依赖服务暂时不可用（仅客服端） | 稍后重试 |

### 3.2 UnwatchTrip

```ts
await connection.invoke('UnwatchTrip', tripId)
```

返回成功外壳（`data` 为 `null`）；只有 tripId 为空时返回 40001。离开页面或关闭弹窗时调用。

### 3.3 生命周期

1. 进入司机地图页（客户）或打开追踪弹窗（客服）时，调用 `WatchTrip`，用返回的 `location` 画首个位置。
2. 监听 `tripLocationUpdated`（§4），收到后移动车辆图标。
3. 断线重连成功后，对当前仍在查看的行程**重新调用** `WatchTrip`，用返回值补上断线期间的位置。服务端不会自动恢复订阅。
4. 离开页面时调用 `UnwatchTrip`。
5. 收到 `TRIP_LOCATION_ENDED` 后，停止更新车辆图标并调用 `UnwatchTrip`。

## 4. 推送：tripLocationUpdated

```ts
connection.on('tripLocationUpdated', (message) => { /* ... */ })
```

该方法与现有的 `notificationReceived` 分开，现有通知处理逻辑不受影响。

### 4.1 位置更新

```json
{
  "code": 20000,
  "message": "success",
  "data": {
    "schemaVersion": 1,
    "eventType": "TRIP_LOCATION_UPDATED",
    "tripId": "trip_0190a1b2c3d4",
    "location": {
      "latitude": 22.198745,
      "longitude": 113.543873,
      "headingDegrees": 132,
      "speedKmh": 36.4,
      "seq": 1287,
      "recordedAt": "2026-10-19T01:12:05.120Z"
    }
  },
  "traceId": "..."
}
```

### 4.2 共享结束

```json
{
  "code": 20000,
  "message": "success",
  "data": {
    "schemaVersion": 1,
    "eventType": "TRIP_LOCATION_ENDED",
    "tripId": "trip_0190a1b2c3d4",
    "location": null
  },
  "traceId": "..."
}
```

### 4.3 处理规则

- 只处理 `data.tripId` 等于当前查看行程的消息。
- `location.seq` 小于等于已显示的序号时丢弃，防止乱序覆盖。
- 同一行程最多约每 3 秒推送一次，前端不需要额外节流；需要平滑动画时，在两点之间插值即可。
- `schemaVersion` 不是 1 时忽略该消息。
- 推送不保证送达。断线期间错过的位置，以重连后 `WatchTrip` 的返回值为准。

## 5. TypeScript 类型参考

```ts
export interface TripLocation {
  latitude: number
  longitude: number
  headingDegrees: number | null
  speedKmh: number | null
  seq: number
  recordedAt: string
}

export interface TripLocationSnapshot {
  tripId: string
  isSharing: boolean
  location: TripLocation | null
}

export interface TripLocationMessage {
  code: number
  message: string
  data: {
    schemaVersion: 1
    eventType: 'TRIP_LOCATION_UPDATED' | 'TRIP_LOCATION_ENDED'
    tripId: string
    location: TripLocation | null
  }
  traceId: string
}

export interface DriverLocationPoint {
  seq: number
  latitude: number
  longitude: number
  accuracyMeters?: number
  headingDegrees?: number
  speedKmh?: number
  recordedAt: string
}

export interface DriverLocationUpload {
  coordinateSystem: 'GCJ02'
  points: DriverLocationPoint[]
}
```

## 6. 各端改动要点

| 端 | 要点 |
|---|---|
| 司机端 | 出发后启动 3 秒定时采集（并立即采集一次）；WGS-84 转 GCJ-02；`seq` 按行程持久化在本地；网络失败时在当前进程中缓存点，下次补发；收到 409 或完成后停止。 |
| 客户端 | 司机地图页进入时 Watch、离开时 Unwatch；监听 `tripLocationUpdated` 移动车辆图标；重连后重新 Watch；没有位置时保持现有路线显示。 |
| 客服控制台 | 追踪弹窗打开时 Watch、关闭时 Unwatch；有真实位置时用它替换现在按时间模拟的车辆进度，没有位置时保留模拟。 |
