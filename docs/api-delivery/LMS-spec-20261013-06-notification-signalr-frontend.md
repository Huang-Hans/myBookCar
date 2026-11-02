# LMS SignalR 通知前端对接契约

创建日期：2026-10-13  
状态：后端已实现并编译通过（未做运行验证），详见 [后端交付说明](../design/LMS-delivery-20261013-07-notification-signalr-backend.md)；Hub 在部署并运行 [Flyway 迁移](../../backend/migrations/README.md)、提供服务身份密钥与收件人角色集合之前不可用于联调，其中客服下单通知依赖这两项配置。
配套：[后端执行计划](../design/LMS-plan-20261013-05-notification-signalr-backend.md)。  
依据：[司机原型](https://city-ideate-98859534.figma.site)、[客户原型](https://frost-wagon-31144857.figma.site)及用户提供的客服截图。

本文仅说明对接契约与页面行为，不包含前端实现步骤。现有 HTTP 接口路径、参数和响应保持不变。

## 1. 连接与方法

地址使用现有 API Gateway 基地址，身份来自现有 Token。

| 接收端 | Hub Path | Token |
| --- | --- | --- |
| 客服 | /api/booking/staff/v1/notifications | 员工 Token |
| 司机 | /api/dispatch/driver/v1/notifications | 司机 Token |
| 客户 | /api/booking/customer/v1/notifications | 客户 Token |

令牌：协商请求使用 Authorization 头；WebSocket 无法设置请求头时使用 access_token 查询参数（仅限上述三个 Hub 路径）。令牌到期或授权被收回后连接会被服务端关闭，需用新令牌重新连接。

| 方法 | 方向 | 用途 |
| --- | --- | --- |
| notificationReceived | 服务端 → 客户端 | 推送统一通知消息 |
| getNotificationSnapshot | 客服 → 服务端 | 只读查询客户下单通知及未读数 |

不额外传用户 ID、司机 ID、客户 ID或订阅组。Hub 不提供下单、派单、接单等业务命令。

## 2. 事件与原型页面

| eventType | 接收端 | 目标 | 页面行为 |
| --- | --- | --- | --- |
| BOOKING_CREATED | 客服 | TRIP / tripId | 更新顶部通知；点击后进入 Dispatch，定位并打开订单详情 |
| DRIVER_OFFER_CREATED | 司机 | DRIVER_OFFER / offerId | 有效邀请显示底部新订单提示条，点击打开 Incoming Request |
| DRIVER_OFFER_CLOSED | 司机 | DRIVER_OFFER / offerId | 清除已经关闭的邀请提示 |
| DRIVER_OFFER_ACCEPTED | 司机 | DRIVER_OFFER / offerId | 接受成功提示及 Active Trip 卡片 |
| DRIVER_ASSIGNED | 客户 | TRIP / tripId | 更新首页行程提示、订单司机信息 |
| TRIP_STATUS_CHANGED | 客户 | TRIP / tripId | 更新首页行程提示和 Status Tracker |

客服仅接收客户自助下单通知。消息到达时不刷新派单列表、计数或业务详情，点击通知后再查询、刷新和定位。

司机保留原型滑动接受控件；客户保留首页提示条和 Trip Details。客户前端另提供跨页面的本次会话通知入口、未读角标及新消息提示，仅缓存当前会话收到的实时事件，不调用客服专用快照方法，也不宣称提供服务端通知历史或已读同步。

## 3. 统一消息

所有字段必填；可空字段明确返回 null。客户端方法 notificationReceived 接收 NotificationMessageV1。

| 字段 | 类型 | 可空 | 说明 |
| --- | --- | --- | --- |
| code | integer | 否 | 固定 20000 |
| message | string | 否 | 固定 success |
| traceId | string | 否 | 业务与投递日志关联 |
| data | object | 否 | 以下通知事件字段的容器 |
| data.eventId | string | 否 | 接收人的稳定事件 ID；重试保持相同值 |
| data.schemaVersion | integer | 否 | 固定 1 |
| data.eventType | string | 否 | 第 2 节六个允许值之一 |
| data.notification | object | 否 | 以下通知内容字段的容器 |
| data.notification.notificationId | string | 是 | 客服站内通知 ID；司机、客户为 null |
| data.notification.type | string | 是 | 客服为 SYSTEM_ALERT；司机、客户为 null |
| data.notification.summary | string | 否 | 纯文本摘要 |
| data.notification.tripReference | string | 是 | 展示用订单号 |
| data.notification.occurredAt | string，date-time | 否 | UTC ISO 8601，Z 后缀 |
| data.notification.targetType | string | 是 | TRIP、DRIVER_OFFER、QUALIFICATION 或 null |
| data.notification.targetId | string | 是 | 业务对象 ID，与 targetType 同时有值或同时为 null |

TRIP 对应 tripId；DRIVER_OFFER 对应 offerId。QUALIFICATION 保留已有目标含义，本次事件不使用。定位使用 targetId，不使用订单编号或摘要文字。

eventType 是实时事件，type 是旧通知分类。BOOKING_CREATED 不代表订单已确认，也不作为旧 HTTP 通知 type 参数。

示例：

~~~json
{
  "code": 20000,
  "message": "success",
  "data": {
    "eventId": "evt_booking_created_trip001_user001",
    "schemaVersion": 1,
    "eventType": "BOOKING_CREATED",
    "notification": {
      "notificationId": "notice_trip001_user001",
      "type": "SYSTEM_ALERT",
      "summary": "New customer booking TR-20261013-001.",
      "tripReference": "TR-20261013-001",
      "occurredAt": "2026-10-13T08:30:00Z",
      "targetType": "TRIP",
      "targetId": "trip_001"
    }
  },
  "traceId": "tr_booking001"
}
~~~

其余五类事件使用相同结构，notificationId/type 为 null，eventType 与目标按第 2 节取值。

## 4. 客服通知快照

getNotificationSnapshot 仅支持客服连接，结果仅包含本人当前可见的 BOOKING_CREATED 通知。旧审批、取消等通知不进入本次顶部入口。

调用时传入一个对象，字段均可省略（`{}` 取全部默认值）；不传对象会被 SignalR 以参数个数不符拒绝。

请求：

| 字段 | 类型 | 必填 | 默认值 | 限制 |
| --- | --- | --- | --- | --- |
| page | integer | 否 | 1 | >=1，不可为 null |
| pageSize | integer | 否 | 20 | 1～100，不可为 null |

固定按 occurredAt 降序、notificationId 升序。

成功结果使用统一 Envelope，code=20000、message=success、traceId 非空：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| data.items | NotificationSnapshotItem[] | 当前页通知，空结果为 [] |
| data.unreadCount | integer | 当前可见下单通知的总未读数，不是当前页数量 |
| meta.pagination.currentPage | integer | 当前页 |
| meta.pagination.pageSize | integer | 每页条数 |
| meta.pagination.totalItems | integer | 通知总数 |
| meta.pagination.totalPages | integer | 总页数，沿用现有空分页语义 |

NotificationSnapshotItem：

| 字段 | 类型 | 可空 | 说明 |
| --- | --- | --- | --- |
| eventId | string | 否 | 稳定事件 ID |
| eventType | string | 否 | 固定 BOOKING_CREATED |
| notification | object | 否 | 第 3 节 notification 结构，notificationId 非空 |
| isRead | boolean | 否 | 已读状态 |
| readAt | string，date-time | 是 | UTC 阅读时间 |

失败结果为现有 ApiErrorEnvelope：code、message、errors[{field,message,code?}]、traceId。可能的 code：40001、40100、40101、40300、50300。Hub 方法结果中的 code 不等同于一次新的 HTTP 状态码。

旧通知中心列表/未读接口仍保留；其结果包含其他通知，不能代替本快照的专用数量。已读操作继续复用现有 HTTP 接口。

## 5. 复用 HTTP 接口

以下为相关接口，全部保持原契约。

| 端 | 接口 | 用途 |
| --- | --- | --- |
| 客服 | POST /api/booking/staff/v1/console-notifications/{notificationId}/read | 点击通知标记已读 |
| 客服 | GET /api/dispatch/staff/v1/dispatch-orders/{tripId} | 查询并定位派单详情 |
| 客服 | GET /api/dispatch/staff/v1/dispatch-orders | 派单列表 |
| 客服 | GET /api/dispatch/staff/v1/dispatch-orders/summaries | 状态计数 |
| 客服 | GET /api/dispatch/staff/v1/dispatch-orders/{tripId}/drivers | 候选司机 |
| 客服 | POST /api/dispatch/staff/v1/dispatch-orders/{tripId}/offers | 发送邀请；正文仍为 driverId，保留现有请求头 |
| 客服 | GET /api/dispatch/staff/v1/dispatch-orders/{tripId}/offers | 邀请历史 |
| 司机 | GET /api/dispatch/driver/v1/me/offers | 有效邀请 |
| 司机 | POST /api/dispatch/driver/v1/me/offers/{offerId}/responses | decision 仍为 ACCEPT / DECLINE |
| 司机 | GET /api/dispatch/driver/v1/me | 司机可用状态 |
| 司机 | GET /api/booking/driver/v1/me/trips | ACTIVE 行程 |
| 司机 | GET /api/booking/driver/v1/me/trips/{tripId} | 行程详情 |
| 客户 | GET /api/booking/customer/v1/me | 当前订单摘要 |
| 客户 | GET /api/booking/customer/v1/me/trips | 本人订单列表 |
| 客户 | GET /api/booking/customer/v1/me/trips/{tripId} | 本人订单详情 |

## 6. 必要状态约定

- 消息可重复或乱序，eventId 用于去重；订单和邀请以现有 HTTP 查询结果为准。
- 断线恢复的数据来源：客服通知快照、司机有效邀请/ACTIVE 行程、客户本人订单。
- 邀请倒计时以 serverTime/expiresAt 为准；原型的 30 秒不作为固定业务值。
- 司机响应 202 / 20200 / ACCEPTING 表示处理中；200 / 20000 / ACCEPTED 才表示接受完成。
- 接受成功后的 Arrived at Pickup 是下一步动作，不表示已经到达。
- 收到通知不等于已读；notificationId=null 时没有客服已读操作。
- 当前只覆盖在线通知，不代表手机后台推送可用。

客户 Status Tracker 映射：

| 订单状态 | 原型展示 |
| --- | --- |
| PENDING | 待确认，不显示已准备接客 |
| CONFIRMED，尚未指派 | 等待司机分配 |
| DRIVER_ASSIGNED | Confirmed；Your Driver Is Preparing for Pickup |
| DRIVER_EN_ROUTE | Driver on the Way |
| ARRIVED_AT_PICKUP | Arrived |
| IN_PROGRESS | In Progress |
| COMPLETED | Completed，移出活跃提示 |
| CANCELLED | Cancelled，移出活跃提示 |

## 7. 原型差异

自动重派、权威车型/容量匹配、独立车牌、邀请完整服务/地址/时长、实时 GPS/ETA/里程及联系渠道仍有缺口，详见后端计划第 12 节。通知接通不代表这些能力完成；不使用原型示例值填充真实数据。
