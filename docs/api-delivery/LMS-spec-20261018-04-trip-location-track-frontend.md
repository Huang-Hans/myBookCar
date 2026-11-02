# LMS 行程行驶轨迹前端契约

创建日期：2026-10-18  
状态：后端已实施（未做运行验证），前端按本契约对接。  
后端计划：[LMS-plan-20261018-03-trip-location-track.md](../design/LMS-plan-20261018-03-trip-location-track.md)  
实时定位契约（本契约在其基础上扩展）：[LMS-spec-20261018-02-trip-live-location-frontend.md](LMS-spec-20261018-02-trip-live-location-frontend.md)

## 1. 适用范围

| 端 | 用到的部分 |
|---|---|
| 客服网页控制台 LMS-User | 全部 |
| 司机端 LMS-DriverApp | 沿用实时定位契约 §2 的上报 API 和字段；按产品要求以前台每 3 秒为周期上报 |
| 客户端 LMS-ClientApp | 不改 |

通用约定与实时定位契约相同：字段使用小驼峰，时间为 ISO 8601 UTC（以 `Z` 结尾），坐标系为 **GCJ-02**。

## 2. 查询行驶轨迹（HTTP）

```
GET /api/booking/staff/v1/trips/{tripId}/track
GET /api/booking/staff/v1/trips/{tripId}/track?afterSeq=1287
Authorization: Bearer <客服令牌>
```

| 查询参数 | 必填 | 说明 |
|---|---|---|
| `afterSeq` | 否 | 整数，≥ 0。只返回 `seq` 大于该值的点，用于增量补拉。不传则返回全部 |

### 2.1 响应

```json
{
  "code": 20000,
  "message": "success",
  "data": {
    "tripId": "trip_0190a1b2c3d4",
    "isSharing": true,
    "sampled": false,
    "points": [
      { "seq": 1, "latitude": 22.198745, "longitude": 113.543873, "headingDegrees": 132, "speedKmh": 36.4, "recordedAt": "2026-10-19T01:12:05.120Z" },
      { "seq": 2, "latitude": 22.198912, "longitude": 113.544101, "headingDegrees": null, "speedKmh": null, "recordedAt": "2026-10-19T01:12:10.118Z" }
    ]
  },
  "meta": { "traceId": "..." },
  "traceId": "..."
}
```

| 字段 | 说明 |
|---|---|
| `isSharing` | 当前是否处于共享窗口（司机已出发且行程未结束），与 `WatchTrip` 含义相同。为 `false` 时不会再有新的点 |
| `sampled` | 为 `true` 时，点数超过 2000，服务端做了等间隔抽样（保留首尾两点），轨迹形状可能略有简化 |
| `points` | 按 `seq` 升序。点的结构与实时推送中的 `location` 完全相同。从未出发的行程返回空数组；已完成或已取消的行程在保留期（默认 30 天）内返回完整轨迹 |

### 2.2 错误

| HTTP / code | 情况 | 前端处理 |
|---|---|---|
| 400 / 40001 | `afterSeq` 非法、未知查询参数 | 检查调用参数 |
| 401 / 40100、40101 | 未认证、令牌过期 | 走现有刷新流程后重试 |
| 403 / 40300 | 缺少 `bookings:read` | 不显示轨迹 |
| 404 / 40400 | 行程不存在或不在物业范围内 | 不显示轨迹 |
| 5xx / 网络失败 | 暂时不可用 | 保留已显示的轨迹，稍后重试 |

## 3. 与实时推送的配合

实时部分完全沿用现有契约：`WatchTrip` / `UnwatchTrip`、`tripLocationUpdated`（`TRIP_LOCATION_UPDATED` / `TRIP_LOCATION_ENDED`），消息格式不变。

### 3.1 生命周期

1. **打开弹窗**：并行调用 `GET .../track` 和 `WatchTrip(tripId)`，把两者返回的点按 `seq` 合并成轨迹。
2. **收到 `TRIP_LOCATION_UPDATED`**：
   - `location.seq` ≤ 当前最大序号：丢弃。
   - `location.seq` = 当前最大序号 + 1：直接追加到轨迹末尾。
   - `location.seq` > 当前最大序号 + 1（中间有缺口，比如推送被限流跳过，或司机端离线补发）：先把这个点追加上去，再调用 `GET .../track?afterSeq=<缺口前的最大序号>` 补齐缺口。
3. **断线重连成功**：重新调用 `WatchTrip`（现有规则），同时调用 `GET .../track?afterSeq=<当前最大序号>`。
4. **收到 `TRIP_LOCATION_ENDED`**：调用一次 `GET .../track?afterSeq=<当前最大序号>` 补齐最后几个点，然后调用 `UnwatchTrip`。轨迹继续显示，车辆图标隐藏。
5. **关闭弹窗**：调用 `UnwatchTrip`，清空轨迹。

### 3.2 合并规则

- 以 `seq` 为键去重，按 `seq` 升序排列。同一 `seq` 已存在时保留已有的点。
- 补拉请求防抖：同一行程同时最多只有一个补拉请求在途；距上次补拉不足 10 秒的缺口先记下，到时间后合并成一次请求。
- 补拉返回 `sampled: true` 时，用返回的点覆盖 `afterSeq` 之后的部分，不要和原有点交错合并。
- 车辆图标的位置取轨迹中 `seq` 最大的点。

### 3.3 显示规则

| 情况 | 显示 |
|---|---|
| 有点、`isSharing` 为 `true` | 行驶折线 + 车辆图标（按 `headingDegrees` 旋转，`null` 时不旋转）+ 上车点、下车点标记 |
| 有点、`isSharing` 为 `false`（已结束） | 行驶折线 + 上车点、下车点标记，不显示车辆图标 |
| 无点、`isSharing` 为 `true` | 上车点、下车点标记，提示"等待定位更新" |
| 无点、`isSharing` 为 `false` | 上车点、下车点标记，提示"暂无行驶轨迹" |
| 40300 / 40400 | 只显示上车点、下车点标记，不显示轨迹和车辆 |
| `sampled` 为 `true` | 额外提示"轨迹已抽样显示" |

- 不再显示高德规划路线，也不再按接车时间模拟车辆位置。
- 视野：首次加载时包住起终点和全部轨迹；用户拖动或缩放地图后，不再自动调整视野。
- 两点之间可以插值做平滑动画，非必需。

## 4. TypeScript 类型参考

```ts
import type { TripLocation } from './contracts'

export interface TripTrack {
  tripId: string
  isSharing: boolean
  sampled: boolean
  points: TripLocation[]
}
```

`TripLocation` 沿用实时定位契约 §5 的定义；校验函数 `isTripTrack` 建议放在 `shared/tripLocations/contracts.ts`，逐个点复用 `isTripLocation`。
