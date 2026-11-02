# LMS 应用码登记

本文登记 LMS 后端已经在代码中使用的五位应用码。格式遵循仓库规范 `HXXNN`：`H` 为 2、4、5，`XX` 为模块族，`NN` 为族内序号。新增应用码必须先在本文登记，再进入代码和 OpenAPI。

代码位置：`backend/src/LMS.BuildingBlocks.Http/Contracts/ApiCodes.cs`（常量）、`Errors/ApiErrors.cs`（HTTP 状态到应用码的映射）。

## 第一阶段已使用

| code | HTTP | message（英文，可展示） | 产生位置 | 说明 |
| --- | --- | --- | --- | --- |
| 20000 | 200 | `success` | `ApiResults.Ok` | 通用成功 |
| 40000 | 400 | `Malformed request` | 请求绑定失败（`BadHttpRequestException`）及框架产生的 400 | 请求无法解析或缺少必填参数 |
| 40000 | 405 | `Method not allowed` | 框架产生的 405 | 沿用 40000，未单独分配码 |
| 40000 | 415 | `Unsupported media type` | 框架产生的 415 | 沿用 40000，未单独分配码 |
| 40000 | 其他 4xx | `Request rejected` | 框架产生的其他无正文 4xx | 兜底，HTTP 状态保持原值 |
| 40001 | 400 | `Validation failed` | `ApiResults.ValidationFailed` | 字段校验失败，`errors[]` 给出 `field` 与 `message` |
| 40400 | 404 | `Resource not found` | 未匹配到路由（网关自身）；业务服务用例结果 `NotFound`（S1 / S2 起，经 `OperationResultMapper`） | 资源或路由不存在，或对调用者不可见 |
| 50000 | 500 | `Internal server error` | 未处理异常边界 | 不含堆栈、路径或配置，详情仅在服务端日志，以 traceId 关联 |
| 50200 | 502 | `Bad gateway` | 网关转发失败 | 仅网关自身产生的转发失败 |
| 50300 | 503 | `Service unavailable` | 网关无可用目标；配置正文不可用（Platform 读取上传配置）；授权数据源不可用（Dispatch / Booking 远程解析调用者权限，`CallerAuthorizationUnavailableException`） | 网关自身产生；业务服务在不能给出真实结果时明确返回，不使用默认或演示数据 |
| 50400 | 504 | `Gateway timeout` | 网关转发超时 | 仅网关自身产生 |

## Portal S1 / S2 新增

随 Platform、Dispatch 的管理接口（S1 / S2）一同启用并登记。代码位置同上，另见 `LMS.BuildingBlocks.Identity`（认证挑战）与 `OperationResultMapper`（用例结果到应用码的映射）。

| code | HTTP | message（英文，可展示） | 产生位置 | 说明 |
| --- | --- | --- | --- | --- |
| 20100 | 201 | `success` | `ApiResults.Created` | 新资源已创建；相同幂等键且内容一致的重放同样返回 201（返回资源当前状态，见交付说明 Q09） |
| 40100 | 401 | `Not authenticated` | 令牌缺失、无效（含签名、issuer、audience、azp 不在 `Identity:AllowedClients` 内）；`Identity:Authority` 未配置时所有业务路由 | 不暴露校验失败原因 |
| 40101 | 401 | `Token expired` | 可识别的令牌过期 | 刷新流程由企业身份决定，不新增刷新接口 |
| 40300 | 403 | `Insufficient permission` | 缺少操作权限；令牌主体未关联可用的应用用户（关联只在网关登录时建立，见下文“网关登录与令牌刷新接口”）；显式选择了授权范围之外的物业；授予超出调用者范围的授权 | 不泄露资源是否存在 |
| 40900 | 409 | `State conflict` | 唯一值重复（执照号、企业邮箱、会员号、地点名称）；幂等键被不同内容复用 | `errors[]` 给出 `Idempotency-Key` 等冲突来源 |

约束说明：

- 40000 用于请求结构错误：JSON 语法错误、空正文、缺少或无法解析的 `Idempotency-Key`，以及详情读取等无正文接口携带了正文。
- 40001 用于字段校验错误：未知 body 字段、类型不符、枚举非法、未知或重复的查询参数、非法的分页与排序参数、合并后违反字段规则。
- 42900 自司机 / 客户本地登录起产生，见下文“司机 / 客户本地认证”；42200 自 S4 起产生，见下一节。

## Portal S3 / S4 新增

物业删除（CF05）复用既有码：有关联员工账号、附件或 Dispatch、Booking、Reporting 业务数据时返回 422 / 42200，`errors[].field` 为 `propertyId`，不删除物业；依赖服务不可用或无法解析其引用结果时返回 503 / 50300，禁止按无引用放行。审计日志和配置历史不作为删除阻断项。

| code | HTTP | message（英文，可展示） | 产生位置 | 说明 |
| --- | --- | --- | --- | --- |
| 42200 | 422 | `Business rule rejected` | `OperationResultMapper`（`BusinessRuleRejected`） | 请求结构合法但业务前置条件未满足：批准前材料未全部核验、客户存在有效限制、指派司机不是 Active（CB06 / CB07，批量时字段为 `items[i].…`）；核验材料时文件不属于该审核材料或安全状态不是 CLEAN（CB04）。`errors[]` 给出失败字段 |

- 已决定的审核/例外再次决定、材料绑定到已决定的审核，使用 40900（`State conflict`），`errors[]` 说明资源当前状态不允许该操作。
- 503 / 50300 在 S3 / S4 另外用于：配额已用量未知时修改限额（QT03）、文件存储未配置（CB05、文件上传）、Platform / Dispatch 不可用而无法完成批准前核对或限制命中读取（CB06 / CB07 / BL05）。
- Booking 的全部业务路由在 Platform 授权数据源不可用时同样返回 503 / 50300（远程解析调用者权限），因此 S3 / S4 的每个 Bearer 保护操作都声明 503。

## Portal S5 / S6 新增

随 Platform 的审计导出（AX01–AX03）、Notification 的通知历史（NT01–NT02）、Reporting 的报表查询、导出与邮件计划（RP01–RP05、EX01–EX03、SC01–SC03）一同启用。

| code | HTTP | message（英文，可展示） | 产生位置 | 说明 |
| --- | --- | --- | --- | --- |
| 20200 | 202 | `success` | `ApiResults.Accepted` / `OperationResultMapper.ToAcceptedResult` | 导出申请（AX01、EX01）已受理，只表示任务已登记为 QUEUED，不表示文件已生成；相同幂等键且内容一致的重放同样返回 202（返回作业当前状态）。结果由 AX02 / EX02 轮询，SUCCEEDED 才表示文件已落地 |

- 404 / 40400：导出（AX02、AX03、EX02、EX03）不存在或不在调用者**当前**授权范围内（读取与下载都按当前授权复核，范围缩小后不能继续获取）；NT02 通知记录不存在。
- 422 / 42200：EX03、AX03 的导出尚未完成（状态不是 SUCCEEDED）或结果已超过保留期。
- 503 / 50300 在 S5 / S6 另外用于：通知投递来源未接入（`NotificationHistory:DeliverySourceConnected` 为 false）；报表读取模型来源未接入（`Reporting:RowsSourceConnected` 为 false），此时报表查询（RP01–RP05）与报表导出申请（EX01）都返回 503，不返回空列表冒充已生成；导出结果文件存储未配置（`FileStorage` 缺项）。来源接入后没有匹配记录才是真实的空结果。
- 403 / 40300：NT01、NT02 的通知记录没有物业归属，可见范围（Q01 / Q11）未确定，目前只有全物业调用者可以读取，限定在单一物业的调用者得到 403。
- 400 / 40001：报表查询与导出申请的 `from`、`to` 都可不传（不限），两者都传时 `to` 不晚于 `from`；导出申请的 `format` 为 `PDF`（服务端 PDF 生成尚未实现）；签名下载与上传地址缺少 `signature` 或 `expires`、或 `expires` 不是整数。签名无效或过期仍为 403 / 40300。
- 409 / 40900：SC02 同一范围内邮箱（忽略大小写）、频率与报表类型重复登记。
- 新增权限码 `reports:read`（RP01–RP05、SC01、EX02、EX03）、`reports:write`（EX01、SC02、SC03）；审计导出 AX01–AX03 复用 `audit:read`；NT01–NT02 使用已有的 `notifications:read`。普通角色的权限仍来自部署配置 `Authorization:RolePermissions`。
- 补充端点（不在合同编号内，由短效签名 URL 授权，不使用 Bearer）：Platform `GET /api/platform/staff/v1/file-downloads/{fileId}`、Reporting `GET /api/reporting/staff/v1/file-downloads/{fileId}`，只提供导出结果文件，成功时响应体是文件内容而不是统一 Envelope。

## 司机端与客户端 P1 新增

随司机端（D01–D07）、客户端（C01–C09）一同启用（原随之启用的司机 / 客户主体绑定管理接口 PB01–PB03 已于 2026-10-12 删除）。**不新增应用码**，只扩展既有码的产生来源；字段级细节见 `openapi/lms-driver.v1.yaml`、`openapi/lms-customer.v1.yaml`。

- 40300（403）：LMS 本地令牌的身份类型与入口不符（司机令牌访问客户接口，反之亦然），或令牌 `sub` 已不对应司机 / 客户记录。员工令牌来自不同发行方，访问司机 / 客户接口为 401 / 40100，不是 40300。司机 / 客户不依赖主体绑定授权，与员工的 `portal_users` 授权互不通用；主体绑定管理接口已删除，库中旧记录保留但不再被读取。
- 40400（404）：他人的行程 / 订单 / 邀请、未指派给本人的行程，与不存在一律不作区分；客户地点详情（C03b）中不存在或已停用的目录地点同样为 404，不返回坐标。
- 40900（409）：邀请已过期、已被拒绝或已被取代，或与已提交的相反决定冲突（D04）；行程当前不是下一允许动作、版本不一致（D07）；同一幂等键被不同动作复用；进行中 / 已终态订单取消（C09）。
- 42200（422）：司机未启用 / 暂停 / 非 AVAILABLE 时接受邀请，或行程中切换工作状态（D02、D04）；自助建单的产品未启用、客户不可自助预订（物业未分配、等级不合格、存在限制）、时间窗、路线不属于所选产品、车型权益与容量、物业配额耗尽（C04、C05）。`errors[]` 的 `field` 给出失败字段。
- 202（20200）：D04 接受邀请时 Booking 的权威指派暂未确认（邀请状态 ACCEPTING），用同一邀请重试不会产生第二笔指派。
- 503（50300）：Platform 目录 / 自助规则、Booking（司机状态对账、权威指派读取）不可用而无法给出真实结果；不返回空列表、默认规则或过期的 ON_TRIP 状态。

## 司机端与客户端 P2 新增

随司机端检查 / 车辆问题 / 偏好 / 运营联系 / 事故报告（D2-01～D2-13）和客户端账户资料 / 收藏 / 评价 / 偏好（C2-01、C2-02、C2-05～C2-11）一同启用。**不新增应用码**，只扩展既有码的产生来源；字段级细节见 `openapi/lms-driver.v1.yaml`、`openapi/lms-customer.v1.yaml`。

- 40001（400）：检查提交缺项、重复项、未知项或未知模板版本；`answers[i].itemCode / result` 非法；车辆问题 / 事故描述为空或超过 2000 字符；`inspectionId`、`tripId` 不是本人的记录；事故 `occurredAt` 晚于服务端当前时间；收藏项不是启用的目录地点、同一分组内重复或超过 `Favorites:MaxItemsPerUsage`；评分不在 1～5、评语超过 1000 个 Unicode 字符；账户 PATCH 携带手机号 / 会员号等不可修改字段（按未知字段拒绝）。
- 40400（404）：他人的检查 / 问题 / 事故记录、不是本人的行程评价，与不存在、尚无评价一律不作区分。
- 40900（409）：本人记录的 `X-Expected-Version` 与当前版本不一致（偏好、账户资料、收藏）；修改已有评价缺少或带错版本（C2-09）；并发的首次保存输家；同一幂等键被不同内容复用（检查 / 问题 / 事故提交）。
- 42200（422）：停用 / 暂停的司机提交检查或登记车辆问题（D2-02、D2-05）；对未完成的行程评价（C2-09）。事故报告不因司机主档状态拒绝。
- 201（20100）：检查、车辆问题、事故报告创建；首次评价（C2-09，版本 1）。相同幂等键 / 相同评价内容的重放同样返回 201。
- 503（50300）：授权数据源、Booking（事故关联行程的归属核对）、Platform 目录（收藏保存时核对地点）不可用而无法给出真实结果；收藏读取时目录不可用只让单项 `isAvailable` 缺省，不使整个读取失败。

## 网关登录与令牌刷新接口

`POST /api/v1/login` 与 `POST /api/v1/refresh` 复用已有应用码，不分配新码：成功为 200 / 20000；请求结构错误为 400 / 40000，字段校验失败为 400 / 40001；非 JSON 请求为 415 / 40000。

- 401 / 40100：Keycloak 以 `invalid_grant` 拒绝凭据（登录）或刷新令牌（刷新：已过期、已撤销或会话已结束，调用方应重新登录），不向调用方暴露上游错误描述。
- 403 / 40300（仅登录）：Keycloak 已接受凭据，但网关用新令牌请求 Platform `POST internal/v1/identity-links` 核对 LMS 用户未通过（只按员工 `portal_users` 核对；司机 / 客户不经 Keycloak 登录，不在此核对）：令牌主体已关联，但令牌中没有已验证企业邮箱或该邮箱与 LMS 记录的 `corporateEmail` 不一致；令牌主体未关联，且没有与已验证企业邮箱相同、未关联且启用的 LMS 用户（含邮箱未验证）；或对应 LMS 用户已停用。此时不返回令牌。核对通过且为首次登录时，Platform 一次性写入 `subjectId` 并记审计。
- Platform 核对请求的连接失败、超时与 503 按下文 502 / 504 / 503 归类，不视为无权限。
- 502 / 50200：身份服务连接失败、客户端或授权方式被拒绝、其他上游失败响应，或令牌响应无法解析、缺少有效令牌字段。
- 503 / 50300：`Login:TokenEndpoint` 未配置，或身份服务返回 503。
- 504 / 50400：身份服务请求超时。调用方主动取消不归类为身份服务超时。

用户名和密码、刷新令牌由调用方输入，网关只配置令牌地址与客户端标识。向 Keycloak 的表单字段遵循其协议；对外响应字段保持 camelCase 与统一 Envelope，全部登录与刷新响应禁止缓存。

## 约定

- 前端依据 `code` 分支，不依据 `message`。`message` 不含内部细节。
- 失败响应的 `errors[]` 非空时，`message` 取 `errors[0].message`（便于前端直接展示具体原因）；`errors[]` 为空时为上表中的默认 message。
- 所有响应都带 `traceId`，响应头同时输出 `X-Trace-Id`，两者一致。
- 网关只为自己产生的错误写 Envelope；下游已经规范化的 JSON 原样透传，不二次包装。
- HEAD、OPTIONS 以及探针 `/healthz`、`/readyz` 不写业务 Envelope。
- 服务间 `/internal/**` 路由不写业务 Envelope（如物业核对成功返回 204 无正文）。
- 42900 仅由司机 / 客户本地登录的失败次数限制产生，见“司机 / 客户本地认证”；其他接口没有限流。

## 司机 / 客户本地认证

随统一认证（`docs/design/LMS-plan-20261011-06-unified-app-authentication.md`）一同启用：司机、客户通过 `POST /api/platform/{driver|customer}/v1/logins`、`token-refreshes`、`logouts` 使用 LMS 本地身份服务签发的令牌（认证方案 `LmsAppBearer`），不经过 Keycloak。员工仍使用 `POST /api/v1/login`、`POST /api/v1/refresh`（认证方案 `StaffBearer`）。字段级细节见 `openapi/lms-driver.v1.yaml`、`openapi/lms-customer.v1.yaml`。

| code | HTTP | message（英文，可展示） | 产生位置 | 说明 |
| --- | --- | --- | --- | --- |
| 42900 | 429 | `Too many requests` | `ApiErrors.TooManyRequests`（Platform 司机 / 客户登录） | 同一手机号（按登录入口的身份类型分别计数）在 15 分钟窗口内连续失败达到 5 次后暂时拒绝；响应头 `Retry-After` 给出等待秒数。计数在 Platform 进程内，多实例各自计数；窗口与次数见 `AppAuthentication` 配置 |

不新增其他应用码，只扩展既有码的产生来源：

- 400 / 40001：`phone` 或 `password` 为空，`phone` 不是含国家 / 地区码的完整手机号（如 `+85361234567`），或仍使用旧字段 `loginId`；刷新令牌为空。身份类型、`role`、`driverId`、`guestId`、`identityProvider` 一律不接受（未知字段为 40001）。
- 401 / 40100：手机号不存在或在所属表中重复、密码错误、密码未设置、司机非 ACTIVE（登录与刷新都按同一消息返回，不区分原因）；刷新令牌无效、过期、属于另一种身份或被重用（重用同时撤销整条刷新链）；业务接口收到缺失、签名 / 发行方 / 受众无效的令牌，或收到员工令牌。
- 401 / 40101：本地访问令牌过期。
- 403 / 40300：本地令牌身份类型与入口不符（司机令牌访问客户接口，反之亦然）。
- 404 / 40400：本人接口访问他人订单 / 行程仍沿用 404。
- 503 / 50300：签名私钥未配置，或所属服务（司机 → Dispatch、客户 → Booking）无法核对凭据；不降级放行、不签发令牌。
- 登录、刷新响应带 `Cache-Control: no-store`；退出成功为 204 无响应体，重复退出同样成功。
- 已签发的访问令牌（默认 15 分钟）在到期前不会被即时撤销；司机停用 / 暂停仍由接单与履约用例按司机表实时状态约束。

## 客服派单

客服派单接口（`/api/dispatch/staff/v1/dispatch-orders`，见 `openapi/lms-console.v1.yaml` 的 Dispatch 标签）不新增应用码，只复用既有码；产生来源如下：

班次看板 `GET /api/dispatch/staff/v1/shifts/board` 支持 `pickupAt`（UTC，精确到秒，例如 `2026-10-30T16:00:00Z`）：先按业务时区确定班次日期及时段，再读取当值司机；凌晨 07:00 前归前一天夜班。时间格式错误或与 `date` 同传返回 400 / 40001。创建派单邀请时，司机未在接载时刻对应班次当值，返回 422 / 42200，字段为 `driverId`。

- 400 / 40001：筛选枚举非法，`to` 不晚于 `from`，`driverId` 为空；`Idempotency-Key` 缺失为 400 / 40000。
- 403 / 40300：缺少 `dispatch:read` / `dispatch:write`，或 `propertyId` 不在授权范围内。
- 404 / 40400：订单或司机不存在，或不在调用者物业范围内。
- 409 / 40900：订单当前不可派单（候选查询）；订单已有占用中的邀请，或所选司机已持有另一张占用中的邀请；订单已被指派、已取消；`X-Expected-Version` 与订单当前派单版本（最近一轮邀请的轮次）不一致；同一 `Idempotency-Key` 对应不同内容。
- 422 / 42200：所选司机不是同物业、未启用、不是 AVAILABLE，或未绑定同物业有效车辆、车辆已停用、车辆等级与订单不符（`errors[].field` 为 `driverId`）。
- 503 / 50300：Booking 无法给出真实结果（不可达、超时、响应无法理解），或按邀请状态筛选 / 计数时订单集合超过 `DispatchQueue:MaxStatusSetSize`；列表不会退化成空队列。

## 实时通知（SignalR）

三个 Hub（`/api/booking/staff/v1/notifications`、`/api/booking/customer/v1/notifications`、`/api/dispatch/driver/v1/notifications`）不新增应用码。协商与握手遵循 SignalR 协议，不套业务 Envelope；Hub 方法的返回值使用统一 Envelope：

- 协商 / 连接阶段：缺失、无效令牌为 401 / 40100，令牌过期为 401 / 40101（Envelope）；令牌身份类型与 Hub 不符为 403；已通过验签但本地记录不存在、已停用或员工权限不足时连接被服务端直接关闭（不返回业务码）。
- `getNotificationSnapshot`（仅客服）：`page` < 1 或 `pageSize` 不在 1～100 为 40001；连接令牌已过期 40101；授权被收回或权限不足 40300；Platform 暂不可用 50300。Hub 方法结果中的 code 不等同于一次新的 HTTP 状态码。
- 内部接口 `POST /internal/v1/notification-recipients/queries`（不经网关）：缺失 / 无效 / 非 Booking 服务身份令牌为 401 / 40100；`eventType` 非 BOOKING_CREATED 或 `propertyId` 为空为 400 / 40001；收件人角色集合未配置或结果超过上限为 503 / 50300（Booking 据此保留事件重试，不当作零收件人）。

## 客服控制台对接（预览、资格申请、时间线）

不新增应用码，只复用既有码；`X-Expected-Version` 在行程修改 / 取消、资格升级 / 批准 / 拒绝等资源上目前**可选且不参与比较**，缺失或不匹配都不会产生 409 / 40900；并发写入的“丢失更新”仍由条件写入兜底并返回 409。

- `POST /api/booking/staff/v1/booking-previews`：业务规则不满足不是错误，而是 200 且 `canSubmit=false`（原因见 `blockedReasons[].code`）；400 / 40001 输入字段缺失或非法；403 越权物业；404 宾客或地点不存在；503 外部来源（Platform / 会员快照）不可用。
- `PATCH /api/booking/staff/v1/guests/{guestId}`：要求 `guests:write`。400 / 40001：空请求、姓名为空或超过 100 字符、房号超过 50 字符、物业标识超过 100 字符、会员等级非法、物业不存在或已删除，以及携带会员号、登录手机号等不可修改字段；403：缺少权限或选择越权物业；404：会员不存在或不在调用者物业范围；503：物业目录不可用。只更新会员快照的姓名、等级、房号、物业，后续上游导入仍可覆盖；不改已有订单快照。
- `POST /api/booking/staff/v1/qualification-references`：400 / 40001 `guestId`、`propertyId`、`justification` 为空；403 物业越权；404 宾客不存在。同一幂等键不同内容为 409 / 40900。
- `POST …/qualification-references/{id}/exception-rejections`：非 ESCALATED 为 409 / 40900；无 `qualifications:approve` 为 403 / 40300。
- `PATCH /api/booking/staff/v1/trips/{id}`：`modificationReason` 非 GUEST_REQUEST / SCHEDULE_CHANGE 为 400 / 40001。
- `POST …/trips/{id}/cancellation`：`comments` 现为可选。
- `GET …/trip-status-summaries`：新增 `search / propertyId / from / to / redemptionState`，`to` 不晚于 `from` 为 400 / 40001。
- `GET /api/platform/staff/v1/locations`：新增 `categoryCode`、`locationIds`；空白 `categoryCode` 为 400 / 40001。

服务产品与车辆等级：客户建单、客服建单及预览在提供 `productCode` 时推导 `vehicleClass`；显式提供的车型与产品配置不一致返回 400 / 40001（`vehicleClass`）。客服旧请求不提供 `productCode` 时仍必须提供 `vehicleClass`。车型权益与容量不足沿用 422 / 42200。
